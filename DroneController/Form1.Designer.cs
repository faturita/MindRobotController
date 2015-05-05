namespace DroneController
{
    partial class Form1
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
            this.connect = new System.Windows.Forms.Button();
            this.Init = new System.Windows.Forms.Button();
            this.turn360 = new System.Windows.Forms.Button();
            this.ftrim = new System.Windows.Forms.Button();
            this.button1 = new System.Windows.Forms.Button();
            this.takeoff = new System.Windows.Forms.Button();
            this.land = new System.Windows.Forms.Button();
            this.SuspendLayout();
            // 
            // connect
            // 
            this.connect.Location = new System.Drawing.Point(387, 11);
            this.connect.Name = "connect";
            this.connect.Size = new System.Drawing.Size(102, 42);
            this.connect.TabIndex = 0;
            this.connect.Text = "Emergency";
            this.connect.UseVisualStyleBackColor = true;
            this.connect.Click += new System.EventHandler(this.connect_Click);
            // 
            // Init
            // 
            this.Init.Location = new System.Drawing.Point(166, 49);
            this.Init.Name = "Init";
            this.Init.Size = new System.Drawing.Size(75, 23);
            this.Init.TabIndex = 1;
            this.Init.Text = "Init";
            this.Init.UseVisualStyleBackColor = true;
            this.Init.Click += new System.EventHandler(this.Init_Click);
            // 
            // turn360
            // 
            this.turn360.Location = new System.Drawing.Point(680, 31);
            this.turn360.Name = "turn360";
            this.turn360.Size = new System.Drawing.Size(75, 23);
            this.turn360.TabIndex = 2;
            this.turn360.Text = "360";
            this.turn360.UseVisualStyleBackColor = true;
            this.turn360.Click += new System.EventHandler(this.turn360_Click);
            // 
            // ftrim
            // 
            this.ftrim.Location = new System.Drawing.Point(680, 111);
            this.ftrim.Name = "ftrim";
            this.ftrim.Size = new System.Drawing.Size(75, 23);
            this.ftrim.TabIndex = 3;
            this.ftrim.Text = "ftrim";
            this.ftrim.UseVisualStyleBackColor = true;
            this.ftrim.Click += new System.EventHandler(this.ftrim_Click);
            // 
            // button1
            // 
            this.button1.Location = new System.Drawing.Point(996, 30);
            this.button1.Name = "button1";
            this.button1.Size = new System.Drawing.Size(75, 23);
            this.button1.TabIndex = 4;
            this.button1.Text = "Reset Seq";
            this.button1.UseVisualStyleBackColor = true;
            this.button1.Click += new System.EventHandler(this.button1_Click);
            // 
            // takeoff
            // 
            this.takeoff.Location = new System.Drawing.Point(387, 136);
            this.takeoff.Name = "takeoff";
            this.takeoff.Size = new System.Drawing.Size(75, 23);
            this.takeoff.TabIndex = 5;
            this.takeoff.Text = "Take Off";
            this.takeoff.UseVisualStyleBackColor = true;
            this.takeoff.Click += new System.EventHandler(this.takeoff_Click);
            // 
            // land
            // 
            this.land.Location = new System.Drawing.Point(387, 166);
            this.land.Name = "land";
            this.land.Size = new System.Drawing.Size(75, 23);
            this.land.TabIndex = 6;
            this.land.Text = "Land";
            this.land.UseVisualStyleBackColor = true;
            this.land.Click += new System.EventHandler(this.land_Click);
            // 
            // Form1
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1151, 276);
            this.Controls.Add(this.land);
            this.Controls.Add(this.takeoff);
            this.Controls.Add(this.button1);
            this.Controls.Add(this.ftrim);
            this.Controls.Add(this.turn360);
            this.Controls.Add(this.Init);
            this.Controls.Add(this.connect);
            this.Name = "Form1";
            this.Text = "AR.Parrot.2";
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.Button connect;
        private System.Windows.Forms.Button Init;
        private System.Windows.Forms.Button turn360;
        private System.Windows.Forms.Button ftrim;
        private System.Windows.Forms.Button button1;
        private System.Windows.Forms.Button takeoff;
        private System.Windows.Forms.Button land;
    }
}

