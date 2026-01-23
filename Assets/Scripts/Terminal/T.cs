// ================================================================================================================================
// File:        CommandTerminal.cs
// Description:	Adds a debug terminal to the game to allow for executing debugging commands
// Author:	    Harley Laurie https://www.github.com/Swaelo/
// ================================================================================================================================

using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;

public class T : MonoBehaviour
{
    //Singleton instance for easy global access
    public static T Instance { get; private set; }
    public static void Log(string Message)
    {
        if(!EnsureInstance())
        {
            Debug.LogWarning($"[Terminal] {Message}");
            return;
        }

        T.Instance.Print(Message);
    }

    //Make sure the static of this class already exists
    private static bool EnsureInstance()
    {
        if(Instance != null)
            return true;

        Instance = FindObjectOfType<T>();

        if(Instance != null)
            return true;

        return false;
    }

    //Groups of commands that need to be registered in
    private SpawningCommands Spawning;
    private NavMeshCommands NavMesh;

    private bool InputActive = false;    //Tracks if the input field is current active
    public static bool IsActive() { return T.Instance.InputActive; }

    [Header("UI")]
    [SerializeField] private GameObject TerminalPanel;  //Root panel that contains the entire console UI elements
    [SerializeField] private TMP_InputField InputField; //Input field where the user can type in commands
    [SerializeField] private TMP_Text OutputText;   //Used to display console output / history
    [SerializeField] private int MaxLines = 8;  //Older lines are dsicarded once this limit is exceeded

    //Dictionary of command names -> command definitions. case-insensitive
    private readonly Dictionary<string, Command> CommandFunctions = new(StringComparer.OrdinalIgnoreCase);

    //Queue of printed lines so we can efficiently drop old ones
    private readonly Queue<string> OutputLines = new();

    //Represents a single console command
    private struct Command
    {
        //Help string shown when using "help <command>"
        public string HelpText;

        //The function executed when the command is called
        //Receives the arguments after the command name
        public Action<string[]> Handler;
    }

    private void Awake()
    {
        //Store reference to self for global access
        Instance = this;

        //Setup all commands that can be executed through the terminal
        DefineBaseCommands();
        DefineGroupCommands();

        //Listen for the input field submit event (Enter key)
        InputField.onSubmit.AddListener(SubmitCommand);
        
    }

    private void Update()
    {
        //Toggle interactivity of the input field based on user input
        ToggleInput();
    }

    //Controls toggling of the input field
    private void ToggleInput()
    {
        //Enable the input field by pressing the back quote key
        if(!InputActive && Input.GetKeyDown(KeyCode.BackQuote))
        {
            StartInput();
            return;
        }

        //Clear and disable the input field by pressing escape
        if(InputActive && Input.GetKeyDown(KeyCode.Escape))
        {
            StopInput();
            ClearInput();
            return;
        }
    }

    //Enables the input field and sets it focus to you can start typing in it
    private void StopInput()
    {
        InputActive = false;
        InputField.interactable = false;
        InputField.DeactivateInputField();
    }
    
    //Exits and clears the input field
    private void StartInput()
    {
        InputActive = true;
        InputField.interactable = true;
        InputField.ActivateInputField();
    }

    //Clears the current content of the input field
    private void ClearInput()
    {
        InputField.text = "";
    }

    //Defines all commands that can be used in the terminal
    private void DefineBaseCommands()
    {
        //Lists all available commands, or details for a specific command
        RegisterNewCommand("help", "Lists commands or: help <command>", Arguments =>
        {
            //If no argument, list all commands
            if(Arguments.Length == 0)
            {
                Print("Commands: " +
                    string.Join(", ", CommandFunctions.Keys.OrderBy(Key => Key)));
                Print("Type: help <command>");
                return;
            }

            //Otherwise show help for a specific command
            var CommandName = Arguments[0];
            if(CommandFunctions.TryGetValue(CommandName, out var Command))
                Print($"{name}: {Command.HelpText}");
            else
                Print($"Unknown command: {CommandName}");
        });

        //Clears all console output
        RegisterNewCommand("clear", "Clears the console output", _ => ClearOutput());

        //Prints world position of a GameObject by name
        RegisterNewCommand("pos", "Print gameobject position: pos <GameObjectName>", Arguments =>
        {
           if(Arguments.Length < 1)
            {
                Print("Usage: pos <GameObjectName>");
                return;
            }

            var FoundObject = GameObject.Find(Arguments[0]);
            if(!FoundObject)
            {
                Print("Not found.");
                return;
            }

            Print($"{FoundObject.name} pos = {FoundObject.transform.position}");
        });
    }

    //Executes registration of any groups of commands that have been defined
    private void DefineGroupCommands()
    {
        //Entity spawning
        Spawning = GetComponent<SpawningCommands>();
        Spawning.RegisterCommands(this);

        //Navmesh management
        NavMesh = GetComponent<NavMeshCommands>();
        NavMesh.RegisterCommands(this);
    }

    //Registers a new console command, can be called from other scripts
    public void RegisterNewCommand(string ChatFunction, string HelpString, Action<string[]> FunctionHandler)
    {
        CommandFunctions[ChatFunction] = new Command
        {
            HelpText = HelpString,
            Handler = FunctionHandler
        };
    }

    //Submits input when the user presses the Enter key
    private void SubmitCommand(string Input)
    {
        //Ignore empty submissions
        if(string.IsNullOrWhiteSpace(Input)) return;

        //Echo to command to the output
        Print($">{Input}");

        //Execute the command
        Execute(Input);

        //Clear input and disable the input
        ClearInput();
        StopInput();
    }

    //Parses and executes a raw command string
    private void Execute(string RawInput)
    {
        //Split input into tokens (command + arguments)
        var InputTokens = Tokenize(RawInput);

        //Exit out of no tokens can be found for some reason
        if(InputTokens.Length == 0) return;

        //First token is the command name and the remaining tokens are arguments
        var CommandName = InputTokens[0];
        var Arguments = InputTokens.Skip(1).ToArray();

        //Loop up the command and try to execute it
        if(CommandFunctions.TryGetValue(CommandName, out var cmd))
        {
            try
            {
                //Execute command handler
                cmd.Handler(Arguments);
            }
            catch (Exception e)
            {
                //Catch runtime errors so console doesn't crash the gamne
                Print($"Error: {e.Message}");
            }
        }
        //Otherwise advise the user the command is unknown
        else
            Print($"Unknown command '{CommandName}'. Type 'help'.");
    }

    //Splits a command string into tokens
    //Supports quoted arguments: e.g.
    //spawn "Big Enemy" 10
    private static string[] Tokenize(string String)
    {
        //Create storage for the tokens
        var Tokens = new List<String>();
        var CurrentToken = "";
        bool InQuotes = false;

        //Loop through and split them all up
        for(int i = 0; i < String.Length; i++)
        {
            //Check if the first character is a quotation for quoted arguments
            var FirstCharacter = String[i];

            //Toggle quoted mode when quotes are encountered
            if(FirstCharacter == '"')
            {
                InQuotes = !InQuotes;
                continue;
            }

            //Only split tokens apart with whitespace while not inside some quotes
            if(!InQuotes && char.IsWhiteSpace(FirstCharacter))
            {
                if(CurrentToken.Length > 0)
                {
                    Tokens.Add(CurrentToken);
                    CurrentToken = "";
                }
            }
            else
                CurrentToken += FirstCharacter;
        }

        //Add the final token
        if(CurrentToken.Length > 0)
            Tokens.Add(CurrentToken);

        //Return the final list
        return Tokens.ToArray();
    }

    //Prints a line to the console output
    public void Print(string Line)
    {
        //Add the new text into the list
        OutputLines.Enqueue(Line);

        //Remove oldest lines if limit exceeded
        while (OutputLines.Count > MaxLines)
            OutputLines.Dequeue();
        
        //Rebuild the output text
        OutputText.text = string.Join("\n", OutputLines);
    }

    //Clears all console output
    private void ClearOutput()
    {
        OutputLines.Clear();
        OutputText.text = "";
    }
}