using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace MnemonicAssignmentCalculator
{
	public partial class MainForm : Form
	{
		#region --Enums--

		private enum State
		{
			Uninitialized,
			Processing,
			Cancelling,
		}

		#endregion --Enums--

		#region --Fields--

		private MnemonicAssigner assigner;
		private State state = State.Uninitialized;

        internal MnemonicAssigner Assigner
        {
            get
            {
                return assigner;
            }

            set
            {
                assigner = value;
            }
        }

        #endregion --Fields--

        #region --Ctor(s) & Setup--

        public MainForm()
		{
			InitializeComponent();

			SetUpForm();
		}

		private void SetUpForm()
		{
			lblDistinctChars.Text = string.Empty;
			lblPermutaionsValue.Text = string.Empty;

			assigner = new MnemonicAssigner();
			lblIncrementMessage.Text = string.Format(MessageResource.IncrementMessageFormat, assigner.ProgressThreshold.ToString("N0"));

			assigner.WorkCompleted += new EventHandler<MnemonicAssigner.WorkCompleteEventArgs>(assigner_WorkCompleted);
			assigner.ProgressChanged += new EventHandler<MnemonicAssigner.WorkerProgressChangedEventArgs>(assigner_ProgressChanged);

			KeyDown += new KeyEventHandler(MainForm_KeyDown);

			btnAction.Click += new System.EventHandler(btnAction_Click);
		}

		#endregion --Ctor(s) & Setup--

		#region --Event Handlers--

		private void MainForm_KeyDown(object sender, KeyEventArgs e)
		{
			if ((e.KeyData == (Keys.Control | Keys.S)) && (state == State.Processing))
			{
				Cancel();
			}
		}

		private void assigner_ProgressChanged(object sender, MnemonicAssigner.WorkerProgressChangedEventArgs e)
		{
			UpdateProgess(e.Threshold, e.ThresholdReachedCount);
		}

		private void assigner_WorkCompleted(object sender, MnemonicAssigner.WorkCompleteEventArgs e)
		{
			UpdateUIForWorkCompleted(e.Success, e.ErrorMessage, e.Cancelled);
		}

		private void btnAction_Click(object sender, EventArgs e)
		{
			ToggleProcessing();
		}

		private void btnBrowse_Click(object sender, EventArgs e)
		{
			BrowseToOutputFile();
		}

		#endregion --Event Handlers--

		#region --Private Methods--

		private void BrowseToOutputFile()
		{
			using (OpenFileDialog fileDialog = new OpenFileDialog())
			{
				DialogResult dialogResult = fileDialog.ShowDialog(this);
				if (dialogResult == System.Windows.Forms.DialogResult.OK)
				{
					txtOutputPath.Text = fileDialog.FileName;
				}
			}
		}

		private void Cancel()
		{
			state = State.Cancelling;
			btnAction.Text = MessageResource.CancellingButtonText;
			btnAction.Enabled = false;
			assigner.CancelWork();
		}

		private void EnableDisableInputControls(bool enabled)
		{
			txtInput.Enabled =
			txtCharsToIgnore.Enabled =
			btnBrowse.Enabled =
			txtOutputPath.Enabled = enabled;
		}

		private List<string> GetCharactersToIgnore()
		{
			List<string> charsToIgnore = new List<string>();
			string[] stringVals = txtCharsToIgnore.Text.Split(',');

			foreach (string stringVal in stringVals)
			{
				string temp = (stringVal ?? string.Empty).Trim();
				if (!string.IsNullOrEmpty(stringVal))
				{
					foreach (char character in temp.ToCharArray())
					{
						string newTemp = character.ToString().Trim();
						if (newTemp.Length > 0)
						{
							charsToIgnore.Add(newTemp);
						}
					}
				}
			}

			return charsToIgnore;
		}

		private List<string> GetItems()
		{
			string separator = ((char)22).ToString();
			string text = txtInput.Text.Trim().Replace(Environment.NewLine, separator);
			List<string> items = text.Split(separator.ToCharArray()).ToList();
			return items;
		}

		private void Initialize()
		{
			if (!ValidateForm())
			{
				return;
			}

			state = State.Processing;

			/* Update UI */
			picProcessing.Image = Properties.Resources.Spin;
			UpdateDistinctCharacterList();
			lblPermutaionsValue.Text = "0";

			EnableDisableInputControls(false);

			btnAction.Enabled = true;
			btnAction.Text = MessageResource.StopWorkButotnText;

			/* Run worker */
			assigner.Process(GetItems(), txtOutputPath.Text.Trim(), GetCharactersToIgnore());
		}

		private void ShowCompletionMessage(bool success, string errorMessage, bool cancelled)
		{
			string message = null;
			MessageBoxIcon icon = MessageBoxIcon.None;
			string header = null;

			if (!string.IsNullOrEmpty(errorMessage))
			{
				message = errorMessage;
				icon = MessageBoxIcon.Error;
				header = MessageResource.ErrorHeader;
			}
			else if (success)
			{
				message = MessageResource.SuccessMessage;
				icon = MessageBoxIcon.Information;
				header = MessageResource.SuccessHeader;
			}
			else if (cancelled)
			{
				message = MessageResource.ProcessingCancelledMessage;
				icon = MessageBoxIcon.Warning;
				header = MessageResource.CancelledHeader;
			}
			else
			{
				message = MessageResource.FailureMessage;
				icon = MessageBoxIcon.Warning;
				header = MessageResource.FailureHeader;
			}

			MessageBox.Show(this, message, header, MessageBoxButtons.OK, icon);
		}

		private void ToggleProcessing()
		{
			switch (state)
			{
				case State.Uninitialized:
					Initialize();
					break;
				case State.Processing:
					Cancel();
					break;
				case State.Cancelling:
				default:
					//Do nothing, wait for cancel to process
					break;
			}
		}

		private void UpdateDistinctCharacterList()
		{
			IList<string> distinctLetters = CharacterLogic.FindUniqueLetters(GetItems()) ?? new List<string>();
			IList<string> charsToIgnore = GetCharactersToIgnore();
			distinctLetters = distinctLetters.Except(charsToIgnore).OrderBy(x => x).ToList();
			lblDistinctChars.Text = (distinctLetters.Count > 0) ? distinctLetters.Aggregate((first, next) => first + ", " + next) : string.Empty;
		}

		private void UpdateProgess(long threshhold, long threshholdReachedCount)
		{
			if (InvokeRequired)
			{
				Action<long, long> delToInvoke = UpdateProgess;
				Invoke(delToInvoke, threshhold, threshholdReachedCount);
			}
			else
			{
				decimal numberOfIterations = threshhold * threshholdReachedCount;
				lblPermutaionsValue.Text = numberOfIterations.ToString("N0");
			}
		}

		private void UpdateUIForWorkCompleted(bool success, string errorMessage, bool cancelled)
		{
			if (InvokeRequired)
			{
				Action<bool, string, bool> delToInvoke = UpdateUIForWorkCompleted;
				Invoke(delToInvoke, success, errorMessage, cancelled);
			}
			else
			{
				picProcessing.Image = null;
				btnAction.Text = MessageResource.DoWorkButtonText;
				state = State.Uninitialized;

				ShowCompletionMessage(success, errorMessage, cancelled);

				EnableDisableInputControls(true);

				btnAction.Enabled = true;
			}
		}

		private bool ValidateForm()
		{
			if (txtInput.Text.Trim().Length == 0)
			{
				MessageBox.Show(MessageResource.InputValuesRequired, Text, MessageBoxButtons.OK, MessageBoxIcon.Warning);
				txtInput.Focus();
				return false;
			}

			if (txtOutputPath.Text.Trim().Length == 0)
			{
				MessageBox.Show(MessageResource.OutputPathRequired, Text, MessageBoxButtons.OK, MessageBoxIcon.Warning);
				txtOutputPath.Focus();
				return false;
			}

			return true;
		}

		#endregion --Private Methods--
	}
}