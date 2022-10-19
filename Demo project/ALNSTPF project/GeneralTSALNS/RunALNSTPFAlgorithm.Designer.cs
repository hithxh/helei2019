namespace GeneralTSALNS
{
    partial class RunALNSTPFAlgorithm
    {
        /// <summary>
        /// 必需的设计器变量。
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// 清理所有正在使用的资源。
        /// </summary>
        /// <param name="disposing">如果应释放托管资源，为 true；否则为 false。</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows 窗体设计器生成的代码

        /// <summary>
        /// 设计器支持所需的方法 - 不要
        /// 使用代码编辑器修改此方法的内容。
        /// </summary>
        private void InitializeComponent()
        {
            this.OAS = new System.Windows.Forms.Button();
            this.AEOSS = new System.Windows.Forms.Button();
            this.TDOPTW = new System.Windows.Forms.Button();
            this.SuspendLayout();
            // 
            // OAS
            // 
            this.OAS.Location = new System.Drawing.Point(100, 21);
            this.OAS.Name = "OAS";
            this.OAS.Size = new System.Drawing.Size(75, 25);
            this.OAS.TabIndex = 2;
            this.OAS.Text = "OAS";
            this.OAS.UseVisualStyleBackColor = true;
            this.OAS.Click += new System.EventHandler(this.OAS_Click);
            // 
            // AEOSS
            // 
            this.AEOSS.Location = new System.Drawing.Point(100, 52);
            this.AEOSS.Name = "AEOSS";
            this.AEOSS.Size = new System.Drawing.Size(75, 25);
            this.AEOSS.TabIndex = 3;
            this.AEOSS.Text = "AEOSS";
            this.AEOSS.UseVisualStyleBackColor = true;
            this.AEOSS.Click += new System.EventHandler(this.AEOSS_Click);
            // 
            // TDOPTW
            // 
            this.TDOPTW.Location = new System.Drawing.Point(100, 83);
            this.TDOPTW.Name = "TDOPTW";
            this.TDOPTW.Size = new System.Drawing.Size(75, 25);
            this.TDOPTW.TabIndex = 4;
            this.TDOPTW.Text = "TDOPTW";
            this.TDOPTW.UseVisualStyleBackColor = true;
            this.TDOPTW.Click += new System.EventHandler(this.TDOPTW_Click);
            // 
            // RunALNSTPFAlgorithm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(277, 136);
            this.Controls.Add(this.TDOPTW);
            this.Controls.Add(this.AEOSS);
            this.Controls.Add(this.OAS);
            this.Name = "RunALNSTPFAlgorithm";
            this.Text = "RunALNSTPFAlgorithm";
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.Button OAS;
        private System.Windows.Forms.Button AEOSS;
        private System.Windows.Forms.Button TDOPTW;
    }
}

