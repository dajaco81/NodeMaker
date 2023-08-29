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

        List<Node> nodes = new List<Node>();

        public Main()
        {
            InitializeComponent();
            StartMovement();
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
                    if (n1 == n2)
                    {
                        continue;
                    }
                    penWidth = Math.Max(0, maxPen - (int)(penSensitivity * getDistance(n1.getPos(), n2.getPos())));
                    alpha = Math.Max(0, maxAlpha - (int)(alphaSensitivity * getDistance(n1.getPos(), n2.getPos())));
                    if (alpha > 0 && penWidth > 0)
                    {
                        g.DrawLine(new Pen(Color.FromArgb(alpha, 0, 255, 255), penWidth), n1.getPos(), n2.getPos());
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

        #region Form Events

        private void FormResize(object sender, EventArgs e)
        {
            foreach (Node n in nodes)
            {
                n.setContainer(canvas.Size);
            }
        }

        private void FormClick(object sender, EventArgs e)
        {
            MouseEventArgs em = (MouseEventArgs)e;
            if (em.Button == MouseButtons.Right)
            {
                nodes.Add(new Node(em.Location, canvas.Size));
            }
        }

        #endregion

    }

    public class Node
    {
        private doubleVector velocity;
        private doubleVector location;
        private Size container;
        private Random r = new Random();

        public Node(Point Location, Size containerSize) : base()
        {
            setContainer(containerSize);

            setVelocity(r.NextDouble() * (5 - -5) + -5, r.NextDouble() * (5 - -5) + -5);

            setPos(Location);
        }

        public void setContainer(Size s)
        {
            container.Width = s.Width;
            container.Height = s.Height;
        }

        public Point getPos()
        { 
            return new Point((int)location.X, (int)location.Y); 
        }

        public void setPos(Point p)
        {
            location.X = p.X;
            location.Y = p.Y;
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
            detectCollisions();
        }

        private void detectCollisions()
        {
            if (location.X < 0)
            {
                velocity.X *= -1;
                location = new doubleVector(0, location.Y);
            }
            if (location.X > container.Width)
            {
                velocity.X *= -1;
                location = new doubleVector(container.Width, location.Y);
            }
            if (location.Y < 0)
            {
                velocity.Y *= -1;
                location = new doubleVector(location.X, 0);
            }
            if (location.Y > container.Height)
            {
                velocity.Y *= -1;
                location = new doubleVector(location.X, container.Height);
            }
        }
    }

    public struct doubleVector
    {
        public double X;
        public double Y;
        public doubleVector(double x, double y)
        {
            X = x;
            Y = y;
        }
    }
}

