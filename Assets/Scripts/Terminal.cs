// ================================================================================================================================
// File:        Terminal.cs
// Description:	Adds a debug terminal to the game to allow for executing debugging commands
// Author:	    Harley Laurie https://www.github.com/Swaelo/
// ================================================================================================================================

using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;

public class Terminal : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private GameObject TerminalPanel;
    [SerializeField] private TMP_InputField InputField;
    [SerializeField] private TMP_Text OutputText;
    [SerializeField] private int MaxLines = 8;

    
}

   // -----------------------------

//     [Header("UI")]

//     // Root panel that contains the entire console UI.
//     // This is what gets toggled on/off.
//     [SerializeField] private GameObject rootPanel;

//     // The TMP input field where the user types commands.
//     [SerializeField] private TMP_InputField inputField;

//     // The TMP text used to display console output / history.
//     [SerializeField] private TMP_Text outputText;

//     // Maximum number of lines kept in the output history.
//     // Older lines are discarded once this limit is exceeded.
//     [SerializeField] private int maxLines = 200;

//     // -----------------------------
//     // Internal data structures
//     // -----------------------------

//     // Dictionary of command name -> command definition.
//     // Case-insensitive so "HELP" == "help".
//     private readonly Dictionary<string, Command> _commands =
//         new(StringComparer.OrdinalIgnoreCase);

//     // Queue of printed lines so we can efficiently drop old ones.
//     private readonly Queue<string> _lines = new();

//     /// <summary>
//     /// Represents a single console command.
//     /// </summary>
//     private struct Command
//     {
//         // Help string shown when using "help <command>"
//         public string Help;

//         // The function executed when the command is called.
//         // Receives the arguments after the command name.
//         public Action<string[]> Handler;
//     }

//     // -----------------------------
//     // Unity lifecycle
//     // -----------------------------

//     private void Awake()
//     {
//         // Ensure the console starts hidden
//         if (rootPanel)
//             rootPanel.SetActive(false);

//         // -----------------------------
//         // Built-in commands
//         // -----------------------------

//         // HELP COMMAND
//         // Lists all available commands, or details for a specific command
//         Register("help", "Lists commands or: help <command>", args =>
//         {
//             // If no argument, list all commands
//             if (args.Length == 0)
//             {
//                 Print("Commands: " +
//                       string.Join(", ", _commands.Keys.OrderBy(k => k)));
//                 Print("Type: help <command>");
//                 return;
//             }

//             // Otherwise show help for a specific command
//             var name = args[0];
//             if (_commands.TryGetValue(name, out var cmd))
//                 Print($"{name}: {cmd.Help}");
//             else
//                 Print($"Unknown command: {name}");
//         });

//         // CLEAR COMMAND
//         // Clears all console output
//         Register("clear", "Clears the console output", _ => ClearOutput());

//         // TIMESCALE COMMAND
//         // Adjusts Unity's Time.timeScale
//         Register("timescale", "Set time scale: timescale <value>", args =>
//         {
//             // Validate input
//             if (args.Length < 1 || !float.TryParse(args[0], out var v))
//             {
//                 Print("Usage: timescale <value>");
//                 return;
//             }

//             Time.timeScale = v;
//             Print($"Time.timeScale = {Time.timeScale}");
//         });

//         // POS COMMAND
//         // Prints world position of a GameObject by name
//         Register("pos", "Print player position: pos <GameObjectName>", args =>
//         {
//             if (args.Length < 1)
//             {
//                 Print("Usage: pos <GameObjectName>");
//                 return;
//             }

//             var go = GameObject.Find(args[0]);
//             if (!go)
//             {
//                 Print("Not found.");
//                 return;
//             }

//             Print($"{go.name} pos = {go.transform.position}");
//         });

//         // Listen for the input field submit event (Enter key)
//         inputField.onSubmit.AddListener(OnSubmit);
//     }

//     private void Update()
//     {
//         // Toggle console visibility with ` or F1
//         if (Input.GetKeyDown(KeyCode.BackQuote) ||
//             Input.GetKeyDown(KeyCode.F1))
//         {
//             Toggle();
//         }

//         // If console is open, force focus back to input field.
//         // Prevents clicking the game and losing typing focus.
//         if (rootPanel &&
//             rootPanel.activeSelf &&
//             !inputField.isFocused)
//         {
//             inputField.ActivateInputField();
//         }
//     }

//     // -----------------------------
//     // Public API
//     // -----------------------------

//     /// <summary>
//     /// Registers a new console command.
//     /// Can be called from other scripts.
//     /// </summary>
//     public void Register(string name, string help, Action<string[]> handler)
//     {
//         _commands[name] = new Command
//         {
//             Help = help,
//             Handler = handler
//         };
//     }

//     /// <summary>
//     /// Toggles the console UI on/off.
//     /// </summary>
//     public void Toggle()
//     {
//         if (!rootPanel) return;

//         var newState = !rootPanel.activeSelf;
//         rootPanel.SetActive(newState);

//         // When opening, clear input and focus it
//         if (newState)
//         {
//             inputField.text = "";
//             inputField.ActivateInputField();
//         }
//     }

//     // -----------------------------
//     // Input handling
//     // -----------------------------

//     /// <summary>
//     /// Called when the user presses Enter in the input field.
//     /// </summary>
//     private void OnSubmit(string text)
//     {
//         // Ignore empty submissions
//         if (string.IsNullOrWhiteSpace(text)) return;

//         // Echo the command to the output
//         Print($"> {text}");

//         // Execute the command
//         Execute(text);

//         // Clear input and keep focus for next command
//         inputField.text = "";
//         inputField.ActivateInputField();
//     }

//     /// <summary>
//     /// Parses and executes a raw command string.
//     /// </summary>
//     private void Execute(string raw)
//     {
//         // Split input into tokens (command + arguments)
//         var tokens = Tokenize(raw);
//         if (tokens.Length == 0) return;

//         // First token is command name
//         var cmdName = tokens[0];

//         // Remaining tokens are arguments
//         var args = tokens.Skip(1).ToArray();

//         // Look up the command
//         if (_commands.TryGetValue(cmdName, out var cmd))
//         {
//             try
//             {
//                 // Execute command handler
//                 cmd.Handler(args);
//             }
//             catch (Exception e)
//             {
//                 // Catch runtime errors so console doesn’t crash
//                 Print($"Error: {e.Message}");
//             }
//         }
//         else
//         {
//             Print($"Unknown command '{cmdName}'. Type 'help'.");
//         }
//     }

//     // -----------------------------
//     // Tokenization
//     // -----------------------------

//     /// <summary>
//     /// Splits a command string into tokens.
//     /// Supports quoted arguments:
//     /// spawn "Big Enemy" 10
//     /// </summary>
//     private static string[] Tokenize(string s)
//     {
//         var result = new List<string>();
//         var current = "";
//         bool inQuotes = false;

//         for (int i = 0; i < s.Length; i++)
//         {
//             var c = s[i];

//             // Toggle quoted mode
//             if (c == '"')
//             {
//                 inQuotes = !inQuotes;
//                 continue;
//             }

//             // Split on whitespace only if not inside quotes
//             if (!inQuotes && char.IsWhiteSpace(c))
//             {
//                 if (current.Length > 0)
//                 {
//                     result.Add(current);
//                     current = "";
//                 }
//             }
//             else
//             {
//                 current += c;
//             }
//         }

//         // Add final token
//         if (current.Length > 0)
//             result.Add(current);

//         return result.ToArray();
//     }

//     // -----------------------------
//     // Output handling
//     // -----------------------------

//     /// <summary>
//     /// Prints a line to the console output.
//     /// Handles line limit and rebuilding the text.
//     /// </summary>
//     private void Print(string line)
//     {
//         // Add new line
//         _lines.Enqueue(line);

//         // Remove oldest lines if limit exceeded
//         while (_lines.Count > maxLines)
//             _lines.Dequeue();

//         // Rebuild output text
//         outputText.text = string.Join("\n", _lines);
//     }

//     /// <summary>
//     /// Clears all console output.
//     /// </summary>
//     private void ClearOutput()
//     {
//         _lines.Clear();
//         outputText.text = "";
//     }
// }