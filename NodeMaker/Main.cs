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
            startMotion();
        }

        #region Timing

        private Timer NodeTicker;
        public void startMotion()
        {
            NodeTicker = new Timer();
            NodeTicker.Tick += new EventHandler(tickNodes);
            NodeTicker.Interval = 10; // in miliseconds
            NodeTicker.Start();
        }

        private void tickNodes(object sender, EventArgs e)
        {
            if (WindowState == FormWindowState.Minimized) return;
            tickNodeLocations();
            renderNodes();
        }

        #endregion

        #region Updates and Rendering
        private void tickNodeLocations()
        {
            foreach (Node n in nodes)
            {
                n.tick();
            }
        }

        Bitmap frameBuffer = new Bitmap(1, 1);

        static readonly Color CanvasColor = Color.Black;
        static readonly Color LineColor = Color.Cyan;

        private void renderNodes()
        {
            frameBuffer = new Bitmap(canvas.Width, canvas.Height);
            Graphics g = Graphics.FromImage(frameBuffer);

            g.Clear(CanvasColor);

            int penAlpha;
            int penWidth;

            int maxAlpha = 100;
            int maxWidth = 3;

            int dropDistance = 330;

            double alphaGrad = (double)maxAlpha / dropDistance;
            double widthGrad = (double)maxWidth / dropDistance;

            double NodeSeparation;

            foreach (Node n1 in nodes)
            {
                foreach (Node n2 in nodes)
                {
                    if (n1 == n2)
                    {
                        continue;
                    }
                    NodeSeparation = getDistance(n1.getPos(), n2.getPos());
                    penAlpha = Math.Max(0, maxAlpha - (int)(alphaGrad * NodeSeparation));
                    penWidth = Math.Max(0, maxWidth - (int)(widthGrad * NodeSeparation));
                    if (penAlpha > 0 && penWidth > 0)
                    {
                        g.DrawLine(new Pen(Color.FromArgb(penAlpha, LineColor), penWidth), n1.getPos(), n2.getPos());
                    }
                }
            }

            canvas.Image = frameBuffer;
        }

        private int getDistance(Point p1, Point p2)
        {
            return (int)Math.Sqrt(Math.Pow((p1.X - p2.X), 2) + Math.Pow((p1.Y - p2.Y), 2));
        }

        private Point getMidCanvas()
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

