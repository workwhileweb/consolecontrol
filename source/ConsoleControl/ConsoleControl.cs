﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using ConsoleControl.Api;

namespace ConsoleControl
{
    /// <summary>
    ///     The console event handler is used for console events.
    /// </summary>
    /// <param name="sender">The sender.</param>
    /// <param name="args">The <see cref="ConsoleEventArgs" /> instance containing the event data.</param>
    public delegate void ConsoleEventHandler(object sender, ConsoleEventArgs args);

    /// <summary>
    ///     The Console Control allows you to embed a basic console in your application.
    /// </summary>
    [ToolboxBitmap(typeof (Resfinder), "ConsoleControl.ConsoleControl.bmp")]
    public partial class ConsoleControl : UserControl
    {
        /// <summary>
        ///     The key mappings.
        /// </summary>
        private readonly List<KeyMapping> _keyMappings = new List<KeyMapping>();

        /// <summary>
        ///     The internal process interface used to interface with the process.
        /// </summary>
        private readonly ProcessInterface _processInteface = new ProcessInterface();

        /// <summary>
        ///     Current position that input starts at.
        /// </summary>
        private int _inputStart = -1;

        /// <summary>
        ///     The is input enabled flag.
        /// </summary>
        private bool _isInputEnabled = true;

        /// <summary>
        ///     The last input string (used so that we can make sure we don't echo input twice).
        /// </summary>
        private string _lastInput;

        /// <summary>
        ///     Initializes a new instance of the <see cref="ConsoleControl" /> class.
        /// </summary>
        public ConsoleControl()
        {
            //  Initialise the component.
            InitializeComponent();

            //  Show diagnostics disabled by default.
            ShowDiagnostics = false;

            //  Input enabled by default.
            IsInputEnabled = true;

            //  Disable special commands by default.
            SendKeyboardCommandsToProcess = false;

            //  Initialise the keymappings.
            InitialiseKeyMappings();

            //  Handle process events.
            _processInteface.OnProcessOutput += processInterace_OnProcessOutput;
            _processInteface.OnProcessError += processInterace_OnProcessError;
            _processInteface.OnProcessInput += processInterace_OnProcessInput;
            _processInteface.OnProcessExit += processInterace_OnProcessExit;

            //  Wait for key down messages on the rich text box.
            InternalRichTextBox.KeyDown += richTextBoxConsole_KeyDown;
        }

        /// <summary>
        ///     Gets or sets a value indicating whether to show diagnostics.
        /// </summary>
        /// <value>
        ///     <c>true</c> if show diagnostics; otherwise, <c>false</c>.
        /// </value>
        [Category("Console Control"), Description("Show diagnostic information, such as exceptions.")]
        public bool ShowDiagnostics { get; set; }

        /// <summary>
        ///     Gets or sets a value indicating whether this instance is input enabled.
        /// </summary>
        /// <value>
        ///     <c>true</c> if this instance is input enabled; otherwise, <c>false</c>.
        /// </value>
        [Category("Console Control"), Description("If true, the user can key in input.")]
        public bool IsInputEnabled
        {
            get { return _isInputEnabled; }
            set
            {
                _isInputEnabled = value;
                if (IsProcessRunning)
                    InternalRichTextBox.ReadOnly = !value;
            }
        }

        /// <summary>
        ///     Gets or sets a value indicating whether [send keyboard commands to process].
        /// </summary>
        /// <value>
        ///     <c>true</c> if [send keyboard commands to process]; otherwise, <c>false</c>.
        /// </value>
        [Category("Console Control"),
         Description("If true, special keyboard commands like Ctrl-C and tab are sent to the process.")]
        public bool SendKeyboardCommandsToProcess { get; set; }

        /// <summary>
        ///     Gets a value indicating whether this instance is process running.
        /// </summary>
        /// <value>
        ///     <c>true</c> if this instance is process running; otherwise, <c>false</c>.
        /// </value>
        [Browsable(false)]
        public bool IsProcessRunning => _processInteface.IsProcessRunning;

        /// <summary>
        ///     Gets the internal rich text box.
        /// </summary>
        [Browsable(false)]
        public RichTextBox InternalRichTextBox { get; private set; }

        /// <summary>
        ///     Gets the process interface.
        /// </summary>
        [Browsable(false)]
        public ProcessInterface ProcessInterface => _processInteface;

        /// <summary>
        ///     Gets the key mappings.
        /// </summary>
        [Browsable(false)]
        public List<KeyMapping> KeyMappings => _keyMappings;

        /// <summary>
        ///     Gets or sets the font of the text displayed by the control.
        /// </summary>
        /// <returns>
        ///     The <see cref="T:System.Drawing.Font" /> to apply to the text displayed by the control. The default is the
        ///     value of the <see cref="P:System.Windows.Forms.Control.DefaultFont" /> property.
        /// </returns>
        /// <PermissionSet>
        ///     <IPermission
        ///         class="System.Security.Permissions.EnvironmentPermission, mscorlib, Version=2.0.3600.0, Culture=neutral, PublicKeyToken=b77a5c561934e089"
        ///         version="1" Unrestricted="true" />
        ///     <IPermission
        ///         class="System.Security.Permissions.FileIOPermission, mscorlib, Version=2.0.3600.0, Culture=neutral, PublicKeyToken=b77a5c561934e089"
        ///         version="1" Unrestricted="true" />
        ///     <IPermission
        ///         class="System.Security.Permissions.SecurityPermission, mscorlib, Version=2.0.3600.0, Culture=neutral, PublicKeyToken=b77a5c561934e089"
        ///         version="1" Flags="UnmanagedCode, ControlEvidence" />
        ///     <IPermission
        ///         class="System.Diagnostics.PerformanceCounterPermission, System, Version=2.0.3600.0, Culture=neutral, PublicKeyToken=b77a5c561934e089"
        ///         version="1" Unrestricted="true" />
        /// </PermissionSet>
        public override Font Font
        {
            get
            {
                //  Return the base class font.
                return base.Font;
            }
            set
            {
                //  Set the base class font...
                base.Font = value;

                //  ...and the internal control font.
                InternalRichTextBox.Font = value;
            }
        }

        /// <summary>
        ///     Gets or sets the background color for the control.
        /// </summary>
        /// <returns>
        ///     A <see cref="T:System.Drawing.Color" /> that represents the background color of the control. The default is
        ///     the value of the <see cref="P:System.Windows.Forms.Control.DefaultBackColor" /> property.
        /// </returns>
        /// <PermissionSet>
        ///     <IPermission
        ///         class="System.Security.Permissions.FileIOPermission, mscorlib, Version=2.0.3600.0, Culture=neutral, PublicKeyToken=b77a5c561934e089"
        ///         version="1" Unrestricted="true" />
        /// </PermissionSet>
        public override Color BackColor
        {
            get
            {
                //  Return the base class background.
                return base.BackColor;
            }
            set
            {
                //  Set the base class background...
                base.BackColor = value;

                //  ...and the internal control background.
                InternalRichTextBox.BackColor = value;
            }
        }

        /// <summary>
        ///     Handles the OnProcessError event of the processInterace control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="args">The <see cref="ProcessEventArgs" /> instance containing the event data.</param>
        private void processInterace_OnProcessError(object sender, ProcessEventArgs args)
        {
            //  Write the output, in red
            WriteOutput(args.Content, Color.Red);

            //  Fire the output event.
            FireConsoleOutputEvent(args.Content);
        }

        /// <summary>
        ///     Handles the OnProcessOutput event of the processInterace control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="args">The <see cref="ProcessEventArgs" /> instance containing the event data.</param>
        private void processInterace_OnProcessOutput(object sender, ProcessEventArgs args)
        {
            //  Write the output, in white
            WriteOutput(args.Content, Color.White);

            //  Fire the output event.
            FireConsoleOutputEvent(args.Content);
        }

        /// <summary>
        ///     Handles the OnProcessInput event of the processInterace control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="args">The <see cref="ProcessEventArgs" /> instance containing the event data.</param>
        private void processInterace_OnProcessInput(object sender, ProcessEventArgs args)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        ///     Handles the OnProcessExit event of the processInterace control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="args">The <see cref="ProcessEventArgs" /> instance containing the event data.</param>
        private void processInterace_OnProcessExit(object sender, ProcessEventArgs args)
        {
            //  Are we showing diagnostics?
            if (ShowDiagnostics)
            {
                WriteOutput(Environment.NewLine + _processInteface.ProcessFileName + " exited.",
                    Color.FromArgb(255, 0, 255, 0));
            }

            if (!IsHandleCreated)
                return;
            //  Read only again.
            Invoke((Action) (() => { InternalRichTextBox.ReadOnly = true; }));
        }

        /// <summary>
        ///     Initialises the key mappings.
        /// </summary>
        private void InitialiseKeyMappings()
        {
            //  Map 'tab'.
            _keyMappings.Add(new KeyMapping(false, false, false, Keys.Tab, "{TAB}", "\t"));

            //  Map 'Ctrl-C'.
            _keyMappings.Add(new KeyMapping(true, false, false, Keys.C, "^(c)", "\x03\r\n"));
        }

        /// <summary>
        ///     Handles the KeyDown event of the richTextBoxConsole control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.Windows.Forms.KeyEventArgs" /> instance containing the event data.</param>
        private void richTextBoxConsole_KeyDown(object sender, KeyEventArgs e)
        {
            //  Are we sending keyboard commands to the process?
            if (SendKeyboardCommandsToProcess && IsProcessRunning)
            {
                //  Get key mappings for this key event?
                var mappings = from k in _keyMappings
                    where
                        (k.KeyCode == e.KeyCode &&
                         k.IsAltPressed == e.Alt &&
                         k.IsControlPressed == e.Control &&
                         k.IsShiftPressed == e.Shift)
                    select k;

                //  Go through each mapping, send the message.
                var keyMappings = mappings as IList<KeyMapping> ?? mappings.ToList();
                foreach (var mapping in keyMappings)
                {
                    //SendKeysEx.SendKeys(CurrentProcessHwnd, mapping.SendKeysMapping);
                    //inputWriter.WriteLine(mapping.StreamMapping);
                    //WriteInput("\x3", Color.White, false);
                }

                //  If we handled a mapping, we're done here.
                if (keyMappings.Any())
                {
                    e.SuppressKeyPress = true;
                    return;
                }
            }

            //  If we're at the input point and it's backspace, bail.
            if ((InternalRichTextBox.SelectionStart <= _inputStart) && e.KeyCode == Keys.Back)
                e.SuppressKeyPress = true;

            //  Are we in the read-only zone?
            if (InternalRichTextBox.SelectionStart < _inputStart)
            {
                //  Allow arrows and Ctrl-C.
                if (!(e.KeyCode == Keys.Left ||
                      e.KeyCode == Keys.Right ||
                      e.KeyCode == Keys.Up ||
                      e.KeyCode == Keys.Down ||
                      (e.KeyCode == Keys.C && e.Control)))
                {
                    e.SuppressKeyPress = true;
                }
            }

            //  Is it the return key?
            if (e.KeyCode != Keys.Return) return;
            //  Get the input.
            var input = InternalRichTextBox.Text.Substring(_inputStart,
                (InternalRichTextBox.SelectionStart) - _inputStart);

            //  Write the input (without echoing).
            WriteInput(input, Color.White, false);
        }

        /// <summary>
        ///     Writes the output to the console control.
        /// </summary>
        /// <param name="output">The output.</param>
        /// <param name="color">The color.</param>
        public void WriteOutput(string output, Color color)
        {
            if (string.IsNullOrEmpty(_lastInput) == false &&
                (output == _lastInput || output.Replace("\r\n", "") == _lastInput))
                return;

            if (!IsHandleCreated)
                return;

            Invoke((Action) (() =>
            {
                //  Write the output.
                InternalRichTextBox.SelectionColor = color;
                InternalRichTextBox.SelectedText += output;
                _inputStart = InternalRichTextBox.SelectionStart;
            }));
        }

        /// <summary>
        ///     Clears the output.
        /// </summary>
        public void ClearOutput()
        {
            InternalRichTextBox.Clear();
            _inputStart = 0;
        }

        /// <summary>
        ///     Writes the input to the console control.
        /// </summary>
        /// <param name="input">The input.</param>
        /// <param name="color">The color.</param>
        /// <param name="echo">if set to <c>true</c> echo the input.</param>
        public void WriteInput(string input, Color color, bool echo)
        {
            Invoke((Action) (() =>
            {
                //  Are we echoing?
                if (echo)
                {
                    InternalRichTextBox.SelectionColor = color;
                    InternalRichTextBox.SelectedText += input;
                    _inputStart = InternalRichTextBox.SelectionStart;
                }

                _lastInput = input;

                //  Write the input.
                _processInteface.WriteInput(input);

                //  Fire the event.
                FireConsoleInputEvent(input);
            }));
        }


        /// <summary>
        ///     Runs a process.
        /// </summary>
        /// <param name="fileName">Name of the file.</param>
        /// <param name="arguments">The arguments.</param>
        public void StartProcess(string fileName, string arguments)
        {
            //  Are we showing diagnostics?
            if (ShowDiagnostics)
            {
                WriteOutput("Preparing to run " + fileName, Color.FromArgb(255, 0, 255, 0));
                if (!string.IsNullOrEmpty(arguments))
                    WriteOutput(" with arguments " + arguments + "." + Environment.NewLine,
                        Color.FromArgb(255, 0, 255, 0));
                else
                    WriteOutput("." + Environment.NewLine, Color.FromArgb(255, 0, 255, 0));
            }

            //  Start the process.
            _processInteface.StartProcess(fileName, arguments);

            //  If we enable input, make the control not read only.
            if (IsInputEnabled)
                InternalRichTextBox.ReadOnly = false;
        }

        /// <summary>
        ///     Stops the process.
        /// </summary>
        public void StopProcess()
        {
            //  Stop the interface.
            _processInteface.StopProcess();
        }

        /// <summary>
        ///     Fires the console output event.
        /// </summary>
        /// <param name="content">The content.</param>
        private void FireConsoleOutputEvent(string content)
        {
            //  Get the event.
            var theEvent = OnConsoleOutput;
            theEvent?.Invoke(this, new ConsoleEventArgs(content));
        }

        /// <summary>
        ///     Fires the console input event.
        /// </summary>
        /// <param name="content">The content.</param>
        private void FireConsoleInputEvent(string content)
        {
            //  Get the event.
            var theEvent = OnConsoleInput;
            theEvent?.Invoke(this, new ConsoleEventArgs(content));
        }

        /// <summary>
        ///     Occurs when console output is produced.
        /// </summary>
        public event ConsoleEventHandler OnConsoleOutput;

        /// <summary>
        ///     Occurs when console input is produced.
        /// </summary>
        public event ConsoleEventHandler OnConsoleInput;
    }

    /// <summary>
    ///     Used to allow us to find resources properly.
    /// </summary>
    public class Resfinder
    {
    }
}