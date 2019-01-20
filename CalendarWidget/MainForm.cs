using CalendarWidget.Properties;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using static CalendarWidget.Calendar;

namespace CalendarWidget
{
    public partial class MainForm : Form
    {
        public MainForm()
        {
            InitializeComponent();
            Opacity = 0.8;

            this.FormBorderStyle = FormBorderStyle.None;
            this.DoubleBuffered = true;
            this.SetStyle(ControlStyles.ResizeRedraw, true);
        }

        int gap = 16;
        int timeWidth = 150;
        const int cGrip = 16;      // Grip size
        const int cCaption = 32;   // Caption bar height;
        List<Label> allLabels = new List<Label>();
        List<Label> resizeLables = new List<Label>();
        int lineGap = 24;
        int y = 0;
        Font dayFont = new Font("Courier", 12, FontStyle.Underline);
        Font font = new Font("Courier", 12);
        Calendar calendar = new Calendar();
        Timer updateTimer = new Timer();

        void MainForm_Load(object sender, EventArgs e)
        {
            Point startPosition = Settings.Default.WindowPosition;
            if (startPosition.X >= 0 && startPosition.Y >= 0)
            {
                Location = startPosition;
            }

            Size = Settings.Default.WindowSize;

            Move += OnMove;
            ResizeEnd += OnResize;

            updateTimer.Tick += OnUpdate;
            updateTimer.Interval = 1000 * 60;
            updateTimer.Start();

            Update();
        }

        void OnUpdate(Object myObject, EventArgs myEventArgs)
        {
            updateTimer.Stop();

            try
            {
                Update();
            }
            catch(Exception e)
            {
                Console.WriteLine(e.ToString());
                Clear();
            }

            updateTimer.Start();
        }

        void Clear()
        {
            foreach (var label in allLabels)
            {
                Controls.Remove(label);
            }
            allLabels.Clear();
            resizeLables.Clear();
        }

        void Update()
        {
            CalendarData data = calendar.GetData(50);

            Clear();

            y = 0;

            foreach (var day in data.Days)
            {
                // Day
                Label dayLabel = new Label()
                {
                    Text = day.Date,
                    Width = Width - (gap + timeWidth),
                    Location = new Point(gap, gap + y),
                    Font = dayFont,
                };
                Controls.Add(dayLabel);
                allLabels.Add(dayLabel);
                resizeLables.Add(dayLabel);
                y += lineGap;

                foreach (var dayEvent in day.Events)
                {
                    // Time
                    Label timeLabel = new Label()
                    {
                        Text = dayEvent.Time,
                        Width = timeWidth,
                        Location = new Point(Width - (gap + timeWidth), gap + y),
                        Font = font,
                        Anchor = AnchorStyles.Top | AnchorStyles.Right,
                        TextAlign = ContentAlignment.MiddleRight,
                    };
                    Controls.Add(timeLabel);
                    allLabels.Add(timeLabel);

                    // Summary
                    Label summaryLabel = new Label()
                    {
                        Text = dayEvent.Summary,
                        Width = Width - (gap + timeWidth),
                        Location = new Point(gap, gap + y),
                        Font = font,
                    };
                    Controls.Add(summaryLabel);
                    allLabels.Add(summaryLabel);
                    resizeLables.Add(summaryLabel);

                    y += lineGap;
                }

                y += lineGap;
            }
        }

        private void canTransformToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Close();
        }

        private void CanTransform(bool enabled)
        {
            quitButton.Checked = enabled;

            if (enabled)
            {
                FormBorderStyle = FormBorderStyle.Sizable;
            }
            else
            {
                FormBorderStyle = FormBorderStyle.None;
            }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Rectangle rc = new Rectangle(this.ClientSize.Width - cGrip, this.ClientSize.Height - cGrip, cGrip, cGrip);
            ControlPaint.DrawSizeGrip(e.Graphics, this.BackColor, rc);
        }

        protected override void WndProc(ref Message m)
        {
            if (m.Msg == 0x84)
            {
                // Trap WM_NCHITTEST
                Point pos = new Point(m.LParam.ToInt32());
                pos = this.PointToClient(pos);
                if (pos.Y < cCaption)
                {
                    m.Result = (IntPtr)2;  // HTCAPTION
                    return;
                }
                if (pos.X >= this.ClientSize.Width - cGrip && pos.Y >= this.ClientSize.Height - cGrip)
                {
                    m.Result = (IntPtr)17; // HTBOTTOMRIGHT
                    return;
                }
            }
            base.WndProc(ref m);
        }

        void OnMove(object sender, EventArgs e)
        {
            Settings.Default.WindowPosition = Location;
            Settings.Default.Save();
        }

        void OnResize(object sender, EventArgs e)
        {
            foreach (var label in resizeLables)
            {
                label.Width = Width - (gap + timeWidth);
            }
            Settings.Default.WindowSize = new Size(Width, Height);
            Settings.Default.Save();
        }
    }
}