using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Windows.Forms;
using UEExplorer.Properties;
using UELib;
using UELib.Core;

namespace UEExplorer.UI.Forms
{
    public partial class HexViewerForm : Form
    {
        private readonly string _FilePath;

        private HexViewerForm()
        {
            InitializeComponent();

            editToolStripMenuItem.Enabled = false;

            WindowState = Settings.Default.HexViewerState;
            Size = Settings.Default.HexViewerSize;
            Location = Settings.Default.HexViewerLocation;
            ViewASCIIItem.Checked = Settings.Default.HexViewer_ViewASCII;
            ViewByteItem.Checked = Settings.Default.HexViewer_ViewByte;
        }

        public HexViewerForm(IBuffered target, string filePath) : this()
        {
            if (target == null)
            {
                throw new NullReferenceException("No target for HexViewDialog()");
            }

            _FilePath = filePath;

            HexPanel.OffsetChangedEvent += (selectedOffset, selectionLength) =>
            {
                int targetPosition = target.GetBufferPosition();
                positionToolStripStatusLabel.Text = $@"0x{targetPosition:X}:{targetPosition + (selectedOffset + selectionLength - 1):X}";

                selectionToolStripStatusLabel.Visible = selectedOffset != -1;
                selectionToolStripStatusLabel.Text = $@"0x{selectedOffset:X}:{selectedOffset + (selectionLength - 1):X} (0x{selectionLength:X} | {selectionLength})";
            };

            HexPanel.BufferModifiedEvent += () =>
            {
                SaveItem.Enabled = true;
            };

            HexPanel.TargetChangedEvent += (_, newTarget) =>
            {
                Text = $@"{Text} - {newTarget.GetBufferId()}";

                SizeLabel.Text = $@"0x{newTarget.GetBufferSize():X}";

                bool canEdit = newTarget is not UnrealPackage;
                editToolStripMenuItem.Enabled = canEdit;
                HexPanel.CanEdit = canEdit;
            };

            HexPanel.SetHexData(target);
        }

        private void HexViewerForm_Load(object sender, EventArgs e)
        {
        }

        private void HexViewDialog_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (WindowState != FormWindowState.Normal)
            {
                Settings.Default.HexViewerLocation = RestoreBounds.Location;
                Settings.Default.HexViewerSize = RestoreBounds.Size;
            }
            else
            {
                Settings.Default.HexViewerLocation = Location;
                Settings.Default.HexViewerSize = Size;
            }

            Settings.Default.HexViewerState = WindowState == FormWindowState.Minimized
                ? FormWindowState.Normal
                : WindowState;
            Settings.Default.Save();
        }

        private void ViewASCIIToolStripMenuItem_CheckedChanged(object sender, EventArgs e)
        {
            HexPanel.DrawASCII = !HexPanel.DrawASCII;
            Settings.Default.HexViewer_ViewASCII = HexPanel.DrawASCII;
            Settings.Default.Save();
        }

        private void ViewByteToolStripMenuItem_CheckedChanged(object sender, EventArgs e)
        {
            HexPanel.DrawByte = !HexPanel.DrawByte;
            Settings.Default.HexViewer_ViewByte = HexPanel.DrawByte;
            Settings.Default.Save();
        }

        private void CopyToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Clipboard.SetText(BitConverter.ToString(HexPanel.Buffer).Replace('-', ' '));
        }

        private void CopyAsViewToolStripMenuItem_Click(object sender, EventArgs e)
        {
            const int firstColumnWidth = 8;
            const int secondColumnWidth = 32;
            const int thirdColumnWidth = 16;
            const string columnMargin = "  ";
            const string byteValueWidth = "  ";
            const char charValueWidth = ' ';
            const char valueMargin = ' ';
            const int columnWidth = 16;

            byte[] buffer = HexPanel.Buffer;
            string input = Resources.HexView_Offset.PadRight(firstColumnWidth, valueMargin) + columnMargin
                + "0  1  2  3  4  5  6  7  8  9  A  B  C  D  E  F ".PadRight(secondColumnWidth, valueMargin) +
                columnMargin
                + "0 1 2 3 4 5 6 7 8 9 A B C D E F ".PadRight(thirdColumnWidth, valueMargin)
                + "\r\n";

            var lines = (int)Math.Ceiling((double)buffer.Length / columnWidth);
            for (var i = 0; i < lines; ++i)
            {
                input += $"\r\n{i * columnWidth:X8}" + columnMargin;
                for (var j = 0; j < columnWidth; ++j)
                {
                    int index = i * columnWidth + j;
                    if (index >= buffer.Length)
                    {
                        input += byteValueWidth;
                    }
                    else
                    {
                        input += $"{buffer[index]:X2}";
                    }

                    if (j < columnWidth - 1)
                    {
                        input += valueMargin;
                    }
                }

                input += columnMargin;

                for (var j = 0; j < columnWidth; ++j)
                {
                    int index = i * columnWidth + j;
                    if (index >= buffer.Length)
                    {
                        input += charValueWidth;
                    }
                    else
                    {
                        input += HexViewerControl.FilterByte(buffer[index]).ToString(CultureInfo.InvariantCulture);
                    }

                    if (j < columnWidth - 1)
                    {
                        input += valueMargin;
                    }
                }
            }

            Clipboard.SetText(input);
        }

        private void ExportBinaryFileToolStripMenuItem_Click(object sender, EventArgs e)
        {
            var fsd = new SaveFileDialog { FileName = HexPanel.Target.GetBufferId() };
            if (fsd.ShowDialog() == DialogResult.OK)
            {
                File.WriteAllBytes(fsd.FileName, HexPanel.Buffer);
            }
        }

        private void ImportBinaryFileToolStripMenuItem_Click(object sender, EventArgs e)
        {
            var target = HexPanel.Target;
            var osd = new OpenFileDialog { FileName = target.GetBufferId() };
            if (osd.ShowDialog() != DialogResult.OK)
            {
                return;
            }

            byte[] buffer = File.ReadAllBytes(osd.FileName);
            if (buffer.Length != HexPanel.Buffer.Length)
            {
                MessageBox.Show(Resources.CANNOT_IMPORT_BINARY_NOTEQUAL_LENGTH);
                return;
            }

            HexPanel.Buffer = buffer;
            HexPanel.Refresh();

            var result = MessageBox.Show(
                Resources.SAVE_QUESTION_WARNING,
                Resources.SAVE_QUESTION,
                MessageBoxButtons.YesNo
            );
            if (result != DialogResult.Yes)
            {
                return;
            }

            if (ReplaceBuffer(target, buffer))
            {
                ReloadObject();
            }
        }

        private void SaveModificationsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (ReplaceBuffer(HexPanel.Target, HexPanel.Buffer))
            {
                SaveItem.Enabled = false;
            }
        }

        private void ReloadPackageToolStripMenuItem_Click(object sender, EventArgs e)
        {
            ReloadPackage();
        }

        private void RedeserializeObjectOnlyUseAtOwnRiskToolStripMenuItem_Click(object sender, EventArgs e)
        {
            ReloadObject();

            // Try renew the buffer, in case modifications were made outside of Hex Viewer.
            ReplaceBuffer(HexPanel.Target, HexPanel.Buffer);
        }

        private void ReloadObject()
        {
            try
            {
                if (HexPanel.Target is UObject obj)
                {
                    obj.Load();
                    HexPanel.Reload();
                }
            }
            catch (Exception e)
            {
                MessageBox.Show(string.Format(Resources.COULDNT_RELOAD_OBJECT, e), Resources.Error,
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void ReloadPackage()
        {
            // TEMP: Hacky solution
            var tab = ((ProgramForm)(Owner)).Tabs.GetTab(_FilePath);
            if (tab != null)
            {
                ((ProgramForm)Owner).Tabs.CloseTab(tab);
            }
            ((ProgramForm)Owner).LoadFromFile(_FilePath);
            Close();
            //_PackageExplorer.ReloadPackage();
        }

        private bool ReplaceBuffer(IBuffered target, byte[] newBuffer)
        {
            string packageFilePath = _FilePath;

            try
            {
                using var fileStream = new FileStream(packageFilePath, FileMode.Open, FileAccess.Write);
                fileStream.Seek(target.GetBufferPosition(), SeekOrigin.Begin);
                fileStream.Write(newBuffer, 0, newBuffer.Length);
                fileStream.Flush();

                return true;
            }
            catch (IOException exc)
            {
                MessageBox.Show(string.Format(Resources.COULDNT_SAVE_EXCEPTION, exc));
            }

            return false;
        }

        private void HelpToolStripMenuItem_Click(object sender, EventArgs e)
        {
            FAQControl.Visible = !FAQControl.Visible;
        }

        private void CopyAddressToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Clipboard.SetText($"0x{HexPanel.Target.GetBufferPosition():X8}");
        }

        private void CopySizeInHexToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Clipboard.SetText($"0x{HexPanel.Target.GetBufferSize():X8}");
        }

        private void HEXWorkshopToolStripMenuItem_Click(object sender, EventArgs e)
        {
            bool appExists = File.Exists(Program.Options.HEXWorkshopAppPath);
            if (!appExists)
            {
                if (MessageBox.Show(string.Format(Resources.PLEASE_SELECT_PATH, "Hex Workshop"),
                        Resources.NOT_AVAILABLE, MessageBoxButtons.OKCancel, MessageBoxIcon.Question) ==
                    DialogResult.Cancel)
                {
                    return;
                }

                var ofd = new OpenFileDialog
                {
                    Filter = "Hex Workshop(HWorks32.exe)|HWorks32.exe",
                    FileName = Path.Combine("%ProgramW6432%", "BreakPoint Software", "Hex Workshop v6", "HWorks32.exe")
                };
                if (ofd.ShowDialog() != DialogResult.OK)
                {
                    return;
                }

                Program.Options.HEXWorkshopAppPath = ofd.FileName;
                Program.SaveConfig();
            }

            string appPath = Program.Options.HEXWorkshopAppPath;
            string filePath = _FilePath;
            int pos = HexPanel.Target.GetBufferPosition();
            int size = HexPanel.Target.GetBufferSize();

            var appArguments = $"\"{filePath}\" /GOTO:{pos} /SELECT:{size}";
            var appInfo = new ProcessStartInfo(appPath, appArguments)
            {
                UseShellExecute = false,
                RedirectStandardOutput = true,
                CreateNoWindow = false
            };
            var app = Process.Start(appInfo);
        }

        private void imHexToolStripMenuItem_Click(object sender, EventArgs e)
        {
            bool appExists = File.Exists(Program.Options.ImHexAppPath);
            if (!appExists)
            {
                if (MessageBox.Show(string.Format(Resources.PLEASE_SELECT_PATH, "ImHex"),
                        Resources.NOT_AVAILABLE, MessageBoxButtons.OKCancel, MessageBoxIcon.Question) ==
                    DialogResult.Cancel)
                {
                    return;
                }

                var ofd = new OpenFileDialog
                {
                    Filter = "ImHex(imhex.exe)|imhex.exe",
                    FileName = Path.Combine("%ProgramW6432%", "ImHex", "imhex.exe")
                };
                if (ofd.ShowDialog() != DialogResult.OK)
                {
                    return;
                }

                Program.Options.ImHexAppPath = ofd.FileName;
                Program.SaveConfig();
            }

            string appPath = Program.Options.ImHexAppPath;
            string filePath = _FilePath;
            int pos = HexPanel.Target.GetBufferPosition();
            int size = HexPanel.Target.GetBufferSize();

            var appArguments = $"--open \"{filePath}\" --select 0x{pos:X} 0x{pos + (size - 1):X}";
            var appInfo = new ProcessStartInfo(appPath, appArguments)
            {
                UseShellExecute = false,
                RedirectStandardOutput = true,
                CreateNoWindow = false
            };
            var app = Process.Start(appInfo);
        }
    }
}
