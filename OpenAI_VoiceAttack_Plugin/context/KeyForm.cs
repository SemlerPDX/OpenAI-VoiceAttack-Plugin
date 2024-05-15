using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using System.Runtime.InteropServices;

namespace OpenAI_VoiceAttack_Plugin
{
    /// <summary>
    /// A class to provide a method for users to supply their OpenAI API Key for the plugin.
    /// </summary>
    /// <para>
    /// <br>OpenAI API VoiceAttack Plugin</br>
    /// <br>Copyright (C) 2023 Aaron Semler</br>
    /// <br><see href="https://github.com/SemlerPDX">github.com/SemlerPDX</see></br>
    /// <br><see href="https://veterans-gaming.com/semlerpdx-avcs">veterans-gaming.com/semlerpdx-avcs</see></br>
    /// <br /><br />
    /// This program is free software: you can redistribute it and/or modify<br />
    /// it under the terms of the GNU General Public License as published by<br />
    /// the Free Software Foundation, either version 3 of the License, or<br />
    /// (at your option) any later version.<br />
    /// <br />
    /// This program is distributed in the hope that it will be useful,<br />
    /// but WITHOUT ANY WARRANTY; without even the implied warranty of<br />
    /// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the<br />
    /// GNU General Public License for more details.<br />
    /// <br />
    /// You should have received a copy of the GNU General Public License<br />
    /// along with this program.  If not, see <see href="https://www.gnu.org/licenses/">gnu.org/licenses</see>.
    /// </para>
    public class KeyForm
    {
        private static readonly string OpenAiWebsite = "https://platform.openai.com/account/api-keys";
        private static readonly string OpenAiPage = "platform.openai.com/account/api-keys";
        private static readonly string ExistingKeyPhrase = "(enter a new key to overwrite key already on file)";

        private static readonly IntPtr HWND_TOPMOST = new IntPtr(-1);
        private const UInt32 SWP_NOSIZE = 0x0001;
        private const UInt32 SWP_NOMOVE = 0x0002;
        private const UInt32 TOPMOST_FLAGS = SWP_NOMOVE | SWP_NOSIZE;

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);

        private static void Footer_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            try
            {
                System.Diagnostics.Process.Start(e.Link.LinkData.ToString());
            }
            catch
            {
                // ...log message, ignore, and continue.
                Logging.WriteToLog_Long("OpenAI Plugin Error: was unable to open link to OpenAI website!", "red");
            }
        }

        private static void SaveKey(string key)
        {
            if (string.IsNullOrEmpty(key) || key == OpenAI_Key.EXAMPLE_API_KEY)
            {
                Logging.WriteToLog_Long("OpenAI Plugin:  An error occurred trying to save your OpenAI API Key!", "red");
                return;
            }

            key = key.Trim();
            OpenAI_Key.ApiKey = key;

            Logging.WriteToLog_Long("OpenAI Plugin:  Success! Your OpenAI API Key has been saved!", "green");

            if (key == ExistingKeyPhrase)
            {
                return;
            }

            OpenAI_Key.SaveToFile(key);
        }

        private static void DeleteKey()
        {
            if (OpenAI_Key.DeleteFromFile())
            {
                Logging.WriteToLog_Long("OpenAI Plugin:  Success! Your OpenAI API Key has been deleted!", "green");
                return;
            }

            Logging.WriteToLog_Long("OpenAI Plugin:  An error occurred trying to delete your OpenAI API Key!", "red");
        }

        /// <summary>
        /// Open a bog-standard dialog box to let users enter their OpenAI API Key and save it to their config folder.
        /// </summary>
        public void ShowKeyInputForm()
        {
            if (OpenAI_Plugin.OpenAiKeyFormOpen)
            {
                Logging.WriteToLog_Long("OpenAI Plugin Message: The Key Menu is already open!", "orange");
                return;
            }

            // Alternate save method - if API Key is provided before calling this context, save to file and exit form.
            string extKey = OpenAI_Plugin.VA_Proxy.GetText("OpenAI_API_Key") ?? string.Empty;
            if (!string.IsNullOrEmpty(extKey))
            {
                SaveKey(extKey);
                OpenAI_Plugin.VA_Proxy.SetText("OpenAI_API_Key", null);
                return;
            }

            OpenAI_Plugin.OpenAiKeyFormOpen = true;
            using (Form keyInputForm = new Form())
            {
                string key = OpenAI_Key.LoadFromFile();

                // Set window in center screen (from mouse) and to be always on top until closed.
                keyInputForm.StartPosition = FormStartPosition.CenterScreen;
                keyInputForm.TopMost = true;
                keyInputForm.FormBorderStyle = FormBorderStyle.FixedSingle;
                keyInputForm.MaximizeBox = false;
                keyInputForm.MinimizeBox = false;
                keyInputForm.BackColor = Color.FromArgb(123, 123, 123);
                keyInputForm.Text = OpenAI_Plugin.VA_DisplayName().Replace("API", "Plugin");

                // Form icon - voiceattack icon will look pretty and professional.
                string iconPath = System.IO.Path.Combine(OpenAI_Plugin.VA_Proxy.InstallDir, "voiceattack.ico") ?? string.Empty;
                if (!string.IsNullOrEmpty(iconPath) && File.Exists(iconPath))
                {
                    keyInputForm.Icon = new Icon(iconPath);
                }

                keyInputForm.MinimumSize = new Size(400, keyInputForm.MinimumSize.Height);

                System.Windows.Forms.Label spacer = new System.Windows.Forms.Label
                {
                    Text = "\n\n",
                    Dock = DockStyle.Top
                };

                System.Windows.Forms.Label label = new System.Windows.Forms.Label();
                label.Text = "Enter your OpenAI API Key to use in this VoiceAttack Plugin:";
                label.Font = new Font(label.Font, FontStyle.Bold);
                label.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
                label.Dock = DockStyle.Top;

                System.Windows.Forms.TextBox textBox = new System.Windows.Forms.TextBox { UseSystemPasswordChar = true };
                textBox.Text = !string.IsNullOrEmpty(key) ? ExistingKeyPhrase : OpenAI_Key.EXAMPLE_API_KEY;
                textBox.TextAlign = HorizontalAlignment.Center;
                textBox.Dock = DockStyle.Top;

                System.Windows.Forms.Button okButton = new System.Windows.Forms.Button();
                okButton.Text = "SAVE";
                okButton.Font = new Font(okButton.Font, FontStyle.Bold);
                okButton.Dock = DockStyle.Bottom;
                okButton.DialogResult = DialogResult.OK; // using to indicate a save request.
                okButton.FlatStyle = FlatStyle.Flat;
                okButton.BackColor = Color.FromArgb(107, 107, 107);
                okButton.FlatAppearance.BorderColor = Color.FromArgb(75, 75, 75);
                okButton.Margin = new Padding(0, 30, 0, 30);

                System.Windows.Forms.Button deleteButton = new System.Windows.Forms.Button();
                deleteButton.Text = "DELETE";
                deleteButton.Font = new Font(deleteButton.Font, FontStyle.Bold);
                deleteButton.Dock = DockStyle.Bottom;
                deleteButton.Location = new Point((keyInputForm.ClientSize.Width - okButton.Width) / 2, okButton.Location.Y);
                deleteButton.DialogResult = DialogResult.Yes; // using to indicate a delete request.
                deleteButton.FlatStyle = FlatStyle.Flat;
                deleteButton.BackColor = Color.FromArgb(107, 107, 107);
                deleteButton.FlatAppearance.BorderColor = Color.FromArgb(75, 75, 75);
                deleteButton.Margin = new Padding(0, 30, 0, 30);

                System.Windows.Forms.Button cancelButton = new System.Windows.Forms.Button();
                cancelButton.Text = "CANCEL";
                cancelButton.Font = new Font(cancelButton.Font, FontStyle.Bold);
                cancelButton.Dock = DockStyle.Bottom;
                cancelButton.DialogResult = DialogResult.Cancel; // ignoring.
                cancelButton.FlatStyle = FlatStyle.Flat;
                cancelButton.BackColor = Color.FromArgb(107, 107, 107);
                cancelButton.FlatAppearance.BorderColor = Color.FromArgb(75, 75, 75);
                cancelButton.Margin = new Padding(0, 30, 0, 30);

                System.Windows.Forms.Label note = new System.Windows.Forms.Label();
                note.Text = "( you can cancel and return to this window anytime, say 'Open the Key Menu' )";
                note.Font = new Font(note.Font, FontStyle.Italic);
                note.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
                note.Dock = DockStyle.Bottom;

                LinkLabel footer = new LinkLabel();
                footer.Text = $"               Find your key here: {OpenAiPage}";
                footer.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
                footer.Links.Add(35, 36, OpenAiWebsite);
                footer.AutoSize = true;
                footer.UseMnemonic = false;
                footer.LinkClicked += new LinkLabelLinkClickedEventHandler(Footer_LinkClicked);
                footer.Dock = DockStyle.Bottom;


                keyInputForm.Controls.Add(textBox);
                keyInputForm.Controls.Add(okButton);
                keyInputForm.Controls.Add(deleteButton);
                keyInputForm.Controls.Add(cancelButton);
                keyInputForm.Controls.Add(note);
                keyInputForm.Controls.Add(footer);
                keyInputForm.Controls.Add(label);
                keyInputForm.Controls.Add(spacer);

                // Bring the form to the foreground as 'always on top' until closed.
                SetWindowPos(keyInputForm.Handle, HWND_TOPMOST, 0, 0, 0, 0, TOPMOST_FLAGS);

                DialogResult result = keyInputForm.ShowDialog();

                if (result == DialogResult.OK)
                {
                    key = textBox.Text;
                    SaveKey(key);
                }
                else if (result == DialogResult.Yes)
                {
                    DeleteKey();
                }
            }

            OpenAI_Plugin.OpenAiKeyFormOpen = false;
        }

    }
}
