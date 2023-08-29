using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Runtime.Remoting.Channels;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace NodeMaker
{
    public partial class Main : Form
    {
        public Main()
        {
            InitializeComponent();
            StartMovement();
        }

        List<Node> nodes = new List<Node>();

        private Node CreateButton(Point location)
        {
            Node n = new Node(location, canvas.Size);
            n.Visible = false;
            n.Click += new EventHandler(NodeClick);
            canvas.Controls.Add(n);
            return n;
        }

        private void NodeClick(object sender, EventArgs e)
        {
            Node n = (Node)sender;
            nodes.Remove(n);
            n.Dispose();
        }

        #region Tick Processing

        private Timer NodeTicker;
        public void StartMovement()
        {
            NodeTicker = new Timer();
            NodeTicker.Tick += new EventHandler(TickAllNodes);
            NodeTicker.Interval = 10; // in miliseconds
            NodeTicker.Start();
        }

        Bitmap frameBuffer = new Bitmap(1, 1);

        private void TickAllNodes(object sender, EventArgs e)
        {
            frameBuffer = new Bitmap(canvas.Width, canvas.Height);
            Graphics g = Graphics.FromImage(frameBuffer);

            g.Clear(Color.Black);

            foreach (Node n in nodes)
            {
                n.tick();
            }

            int alpha;
            int penWidth;

            int maxAlpha = 100;
            int maxPen = 3;

            double alphaSensitivity = 0.3;

            double penSensitivity = maxPen * alphaSensitivity / maxAlpha;

            foreach (Node n1 in nodes) 
            {
                foreach (Node n2 in nodes)
                {
                    if (n1.Location == n2.Location)
                    {
                        continue;
                    }
                    penWidth = Math.Max(0, maxPen - (int)(penSensitivity * getDistance(n1.GetCentre(), n2.GetCentre())));
                    alpha = Math.Max(0, maxAlpha - (int)(alphaSensitivity * getDistance(n1.GetCentre(), n2.GetCentre())));
                    if (alpha > 0 && penWidth > 0)
                    {
                        g.DrawLine(new Pen(Color.FromArgb(alpha, 0, 255, 255), penWidth), n1.GetCentre(), n2.GetCentre());
                    }
                }
            }

            canvas.Image = frameBuffer;
        }

        private int getDistance(Point p1, Point p2)
        {
            return (int)Math.Sqrt(Math.Pow((p1.X - p2.X), 2) + Math.Pow((p1.Y - p2.Y), 2));
        }

        private Point GetMidCanvas()
        {
            return new Point(canvas.Width / 2, canvas.Height / 2);
        }

        #endregion

        private void FormResize(object sender, EventArgs e)
        {
            foreach (Node n in nodes)
            {
                n.SetContainer(canvas.Size);
            }
        }

        private void FormClick(object sender, EventArgs e)
        {
            MouseEventArgs em = (MouseEventArgs)e;
            if (em.Button == MouseButtons.Right)
            {
                nodes.Add(CreateButton(em.Location));
            }
        }
    }

    public class Node : Button
    {
        private doubleVector velocity;
        private doubleVector location;
        private intVector container;
        private Random r = new Random();

        

        public Node(Point Location, Size containerSize) : base()
        {
            SetContainer(containerSize);
            setVelocity(r.NextDouble() * (5 - -5) + -5, r.NextDouble() * (5 - -5) + -5);

            init();
            
            setPos(Location);
        }

        public void SetContainer(Size s)
        {
            container.X = s.Width;
            container.Y = s.Height;
        }

        private void init()
        {
            Width = 10;
            Height = 10;
            BackColor = Color.LightGray;
            Text = "";
        }

        public Point GetCentre()
        { 
            return new Point(Left + Width / 2, Top + Height / 2); 
        }

        public void setPos(int x, int y)
        {
            Location = new Point(x - Width / 2, y - Height / 2);
        }

        public void setPos(Point p)
        {
            Location = new Point(p.X - Width / 2, p.Y - Height / 2);
            location.X = Location.X;
            location.Y = Location.Y;
        }

        public void setVelocity(double x, double y)
        {
            velocity.X = x;
            velocity.Y = y;
        }

        public void tick()
        {
            location.X += velocity.X;
            location.Y += velocity.Y;
            Location = new Point((int)location.X, (int)location.Y);
            detectCollisions();
        }

        private void detectCollisions()
        {
            if (Location.X < 0)
            {
                velocity.X *= -1;
                Location = new Point(0, Location.Y);
            }
            if (Location.X + Width > container.X)
            {
                velocity.X *= -1;
                Location = new Point(container.X - Width, Location.Y);
            }
            if (Location.Y < 0)
            {
                velocity.Y *= -1;
                Location = new Point(Location.X, 0);
            }
            if (Location.Y + Height > container.Y)
            {
                velocity.Y *= -1;
                Location = new Point(Location.X, container.Y - Height);
            }
        }
    }

    public struct intVector
    {
        public int X;
        public int Y;
    }

    public struct doubleVector
    {
        public double X;
        public double Y;
    }
}

