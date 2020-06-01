namespace ProjetoFinalTopSeg
{
    partial class FormClient1
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
            this.tbChat = new System.Windows.Forms.TextBox();
            this.tbMensagem = new System.Windows.Forms.TextBox();
            this.btEnviar = new System.Windows.Forms.Button();
            this.SuspendLayout();
            // 
            // tbChat
            // 
            this.tbChat.Location = new System.Drawing.Point(12, 12);
            this.tbChat.Multiline = true;
            this.tbChat.Name = "tbChat";
            this.tbChat.ReadOnly = true;
            this.tbChat.Size = new System.Drawing.Size(352, 249);
            this.tbChat.TabIndex = 0;
            // 
            // tbMensagem
            // 
            this.tbMensagem.Location = new System.Drawing.Point(12, 267);
            this.tbMensagem.Multiline = true;
            this.tbMensagem.Name = "tbMensagem";
            this.tbMensagem.Size = new System.Drawing.Size(271, 48);
            this.tbMensagem.TabIndex = 1;
            // 
            // btEnviar
            // 
            this.btEnviar.Location = new System.Drawing.Point(289, 265);
            this.btEnviar.Name = "btEnviar";
            this.btEnviar.Size = new System.Drawing.Size(75, 50);
            this.btEnviar.TabIndex = 2;
            this.btEnviar.Text = "Enviar";
            this.btEnviar.UseVisualStyleBackColor = true;
            this.btEnviar.Click += new System.EventHandler(this.btEnviar_Click);
            // 
            // FormClient1
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(378, 329);
            this.Controls.Add(this.btEnviar);
            this.Controls.Add(this.tbMensagem);
            this.Controls.Add(this.tbChat);
            this.Name = "FormClient1";
            this.Text = "CLIENT 1";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.FormClient1_FormClosing);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.TextBox tbChat;
        private System.Windows.Forms.TextBox tbMensagem;
        private System.Windows.Forms.Button btEnviar;
    }
}

