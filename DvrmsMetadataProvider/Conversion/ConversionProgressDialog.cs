// Stephen Toub
// stoub@microsoft.com

using System;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Reflection;
using System.Windows.Forms;

using Toub.MediaCenter.Dvrms.Conversion;

namespace Toub.MediaCenter.Tools.UI
{
	/// <summary>Dialog used to perform a conversion and show the conversion's progress.</summary>
	public sealed class ConversionProgressDialog : System.Windows.Forms.Form
	{
		private System.Windows.Forms.ProgressBar bar;
		private System.Windows.Forms.Button btnCancel;
		private System.Windows.Forms.Label lblTime;
		private System.ComponentModel.Container components = null;

		/// <summary>Initializes the progress dialog.</summary>
		public ConversionProgressDialog() { InitializeComponent(); }

		/// <summary>Disposes of unmanaged resources.</summary>
		/// <param name="disposing">Whether being called due to a call to IDisposable.Dispose.</param>
		protected override void Dispose( bool disposing )
		{
			if( disposing )
			{
				if(components != null) components.Dispose();
			}
			base.Dispose( disposing );
		}

		#region Windows Form Designer generated code
		/// <summary>
		/// Required method for Designer support - do not modify
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
		{
			this.bar = new System.Windows.Forms.ProgressBar();
			this.btnCancel = new System.Windows.Forms.Button();
			this.lblTime = new System.Windows.Forms.Label();
			this.SuspendLayout();
			// 
			// bar
			// 
			this.bar.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
				| System.Windows.Forms.AnchorStyles.Right)));
			this.bar.ForeColor = System.Drawing.SystemColors.Highlight;
			this.bar.Location = new System.Drawing.Point(8, 8);
			this.bar.Name = "bar";
			this.bar.Size = new System.Drawing.Size(374, 23);
			this.bar.Step = 1;
			this.bar.TabIndex = 0;
			// 
			// btnCancel
			// 
			this.btnCancel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this.btnCancel.FlatStyle = System.Windows.Forms.FlatStyle.System;
			this.btnCancel.Location = new System.Drawing.Point(308, 28);
			this.btnCancel.Name = "btnCancel";
			this.btnCancel.TabIndex = 0;
			this.btnCancel.Text = "Cancel";
			this.btnCancel.Click += new System.EventHandler(this.btnCancel_Click);
			// 
			// lblTime
			// 
			this.lblTime.Location = new System.Drawing.Point(8, 40);
			this.lblTime.Name = "lblTime";
			this.lblTime.Size = new System.Drawing.Size(296, 16);
			this.lblTime.TabIndex = 1;
			// 
			// ConversionProgressDialog
			// 
			this.ClientSize = new System.Drawing.Size(390, 62);
			this.MaximumSize = new System.Drawing.Size(1200, 88);
			this.MinimumSize = new System.Drawing.Size(250, 88);
			this.ControlBox = false;
			this.Controls.Add(this.btnCancel);
			this.Controls.Add(this.lblTime);
			this.Controls.Add(this.bar);
			this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.SizableToolWindow;
			this.Name = "ConversionProgressDialog";
			this.ShowInTaskbar = false;
			this.Text = "ConversionProgressDialog";
			this.ResumeLayout(false);
		}
		#endregion

		private Converter _converter;
		private DateTime _start;

		/// <summary>Show the conversion dialog and start the conversion process.</summary>
		/// <param name="parent">Parent window for the dialog.</param>
		/// <param name="converter">The converter to use for conversion.</param>
		public void ShowDialog(IWin32Window parent, Converter converter)
		{
			if (converter == null) throw new ArgumentNullException("converter");
			
			// Store the starting time for "time remaining" purposes
			_start = DateTime.Now;
		
			// Store, setup, and run asynchronously the converter
			_converter = converter;
			_converter.ProgressChanged += new Toub.MediaCenter.Dvrms.Conversion.ProgressChangedEventHandler(converter_ProgressChanged);
			_converter.ConversionComplete += new ConversionCompletedEventHandler(converter_ConversionComplete);
			_converter.ConvertAsync();

			// Show the progress dialog
			ShowDialog(parent);
		}

		/// <summary>Called when the form is closing.</summary>
		/// <param name="e">Cancelation arguments.</param>
		protected override void OnClosing(CancelEventArgs e)
		{
			// Undo event registrations
			if (_converter != null)
			{
				_converter.ProgressChanged -= new Toub.MediaCenter.Dvrms.Conversion.ProgressChangedEventHandler(converter_ProgressChanged);
				_converter.ConversionComplete -= new ConversionCompletedEventHandler(converter_ConversionComplete);
				_converter = null;
			}

			// Deter to base implementation
			base.OnClosing(e);
		}
		
		private void converter_ProgressChanged(object sender, Toub.MediaCenter.Dvrms.Conversion.ProgressChangedEventArgs e)
		{
			if (this.InvokeRequired)
			{
				Invoke(new Toub.MediaCenter.Dvrms.Conversion.ProgressChangedEventHandler(converter_ProgressChanged), new object[]{sender, e});
				return;
			}

			// Let the user know how much time is left
			bar.Value = (int)e.ProgressPercentage;
			TimeSpan remaining = ComputeTimeRemaining(_start, DateTime.Now, e.ProgressPercentage);
			lblTime.Text = "Time Remaining: " + remaining.ToString();
		}

		private void converter_ConversionComplete(object sender, ConversionCompletedEventArgs e)
		{
			// Close the form on completion, notifying the user of any errors in the process
			if (e.Error != null)
			{
				MessageBox.Show(this, e.Error.Message,
					Assembly.GetEntryAssembly().GetName().Name, 
					MessageBoxButtons.OK, MessageBoxIcon.Error);
			}
			Close();
		}

		private static TimeSpan ComputeTimeRemaining(DateTime start, DateTime end, double percent)
		{
			// Estimate how much time is left based on how much time has passed and
			// how much we've completed thus far
			if (percent <= 0 || double.IsInfinity(percent) || double.IsNaN(percent)) return TimeSpan.FromSeconds(0);
			double secondsThusFar = (end - start).TotalSeconds;
			double secondsPerPercent = secondsThusFar / percent;
			return TimeSpan.FromSeconds((int)Math.Abs(secondsPerPercent * (100-percent)));
		}

		private void btnCancel_Click(object sender, System.EventArgs e)
		{
			if (_converter != null) _converter.CancelAsync();
			Close();
		}
	}
}