namespace PutioFS.Windows
{
    partial class SettingsForm
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.LoginOKButton = new System.Windows.Forms.Button();
            this.LoginCancelButton = new System.Windows.Forms.Button();
            this.label1 = new System.Windows.Forms.Label();
            this.LoginApiKey = new System.Windows.Forms.TextBox();
            this.LoginPutioSecret = new System.Windows.Forms.TextBox();
            this.label2 = new System.Windows.Forms.Label();
            this.OpenAtLogin = new System.Windows.Forms.CheckBox();
            this.SuspendLayout();
            // 
            // LoginOKButton
            // 
            this.LoginOKButton.DialogResult = System.Windows.Forms.DialogResult.OK;
            this.LoginOKButton.Location = new System.Drawing.Point(137, 98);
            this.LoginOKButton.Name = "LoginOKButton";
            this.LoginOKButton.Size = new System.Drawing.Size(75, 23);
            this.LoginOKButton.TabIndex = 3;
            this.LoginOKButton.Text = "OK";
            this.LoginOKButton.UseVisualStyleBackColor = true;
            this.LoginOKButton.Click += new System.EventHandler(this.button1_Click);
            // 
            // LoginCancelButton
            // 
            this.LoginCancelButton.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.LoginCancelButton.Location = new System.Drawing.Point(8, 98);
            this.LoginCancelButton.Name = "LoginCancelButton";
            this.LoginCancelButton.Size = new System.Drawing.Size(75, 23);
            this.LoginCancelButton.TabIndex = 4;
            this.LoginCancelButton.Text = "Cancel";
            this.LoginCancelButton.UseVisualStyleBackColor = true;
            this.LoginCancelButton.Click += new System.EventHandler(this.button2_Click);
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(35, 22);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(48, 13);
            this.label1.TabIndex = 2;
            this.label1.Text = "API Key:";
            this.label1.Click += new System.EventHandler(this.label1_Click);
            // 
            // LoginApiKey
            // 
            this.LoginApiKey.AcceptsReturn = true;
            this.LoginApiKey.AcceptsTab = true;
            this.LoginApiKey.AccessibleDescription = "";
            this.LoginApiKey.AccessibleName = "";
            this.LoginApiKey.Location = new System.Drawing.Point(93, 19);
            this.LoginApiKey.Name = "LoginApiKey";
            this.LoginApiKey.Size = new System.Drawing.Size(119, 20);
            this.LoginApiKey.TabIndex = 0;
            this.LoginApiKey.Tag = "";
            // 
            // LoginPutioSecret
            // 
            this.LoginPutioSecret.AcceptsReturn = true;
            this.LoginPutioSecret.AcceptsTab = true;
            this.LoginPutioSecret.AccessibleDescription = "Please enter your Put.io API Key";
            this.LoginPutioSecret.AccessibleName = "Put.io API Key";
            this.LoginPutioSecret.Location = new System.Drawing.Point(93, 45);
            this.LoginPutioSecret.Name = "LoginPutioSecret";
            this.LoginPutioSecret.Size = new System.Drawing.Size(119, 20);
            this.LoginPutioSecret.TabIndex = 1;
            this.LoginPutioSecret.UseSystemPasswordChar = true;
            this.LoginPutioSecret.TextChanged += new System.EventHandler(this.LoginPutioSecret_TextChanged);
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(12, 48);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(71, 13);
            this.label2.TabIndex = 5;
            this.label2.Text = "Put.io Secret:";
            this.label2.Click += new System.EventHandler(this.label2_Click);
            // 
            // OpenAtLogin
            // 
            this.OpenAtLogin.AutoSize = true;
            this.OpenAtLogin.CheckAlign = System.Drawing.ContentAlignment.MiddleRight;
            this.OpenAtLogin.Location = new System.Drawing.Point(15, 75);
            this.OpenAtLogin.Name = "OpenAtLogin";
            this.OpenAtLogin.Size = new System.Drawing.Size(89, 17);
            this.OpenAtLogin.TabIndex = 2;
            this.OpenAtLogin.Text = "Open at login";
            this.OpenAtLogin.UseVisualStyleBackColor = true;
            this.OpenAtLogin.CheckedChanged += new System.EventHandler(this.checkBox1_CheckedChanged);
            // 
            // AuthForm
            // 
            this.AcceptButton = this.LoginOKButton;
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.CancelButton = this.LoginCancelButton;
            this.ClientSize = new System.Drawing.Size(224, 133);
            this.Controls.Add(this.OpenAtLogin);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.LoginPutioSecret);
            this.Controls.Add(this.LoginApiKey);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.LoginCancelButton);
            this.Controls.Add(this.LoginOKButton);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.Name = "AuthForm";
            this.SizeGripStyle = System.Windows.Forms.SizeGripStyle.Show;
            this.Text = "Put.io Authentication";
            this.Load += new System.EventHandler(this.LoginForm_Load);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Button LoginOKButton;
        private System.Windows.Forms.Button LoginCancelButton;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Label label2;
        public System.Windows.Forms.TextBox LoginApiKey;
        public System.Windows.Forms.TextBox LoginPutioSecret;
        private System.Windows.Forms.CheckBox OpenAtLogin;
    }
}