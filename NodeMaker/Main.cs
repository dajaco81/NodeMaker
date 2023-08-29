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

        #region Init

        List<Node> nodes = new List<Node>();

        public Main()
        {
            InitializeComponent();
            initEdgeVals();
        }

        private void Main_Load(object sender, EventArgs e)
        {
            renderWrapper(initCanvas, flush:true);
        }

        #endregion

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
            renderWrapper(renderNodes);
        }

        #endregion

        #region Updates and Rendering

        Bitmap frameBuffer = new Bitmap(1, 1);

        static readonly Color CanvasColor = Color.Black;
        static readonly Color LineColor = Color.Cyan;

        #region Render Wrappers

        private void renderWrapper(Action<Graphics> f, bool flush = false)
        {
            Graphics g;
            if (flush)
            {
                frameBuffer = new Bitmap(canvas.Width, canvas.Height);
                g = Graphics.FromImage(frameBuffer);
                g.Clear(CanvasColor);
            }
            else
            {
                g = Graphics.FromImage(frameBuffer);
            }

            f(g);

            canvas.Image = frameBuffer;
        }

        private void renderWrapper(Node n, Action<Node, Graphics> f, bool flush = false)
        {
            Graphics g;
            if (flush)
            {
                frameBuffer = new Bitmap(canvas.Width, canvas.Height);
                g = Graphics.FromImage(frameBuffer);
                g.Clear(CanvasColor);
            }
            else
            {
                g = Graphics.FromImage(frameBuffer);
            }

            f(n, g);

            canvas.Image = frameBuffer;
        }

        private void renderWrapper(Node n1, Node n2, Action<Node, Node, Graphics> f, bool flush = false)
        {
            Graphics g;
            if (flush)
            {
                frameBuffer = new Bitmap(canvas.Width, canvas.Height);
                g = Graphics.FromImage(frameBuffer);
                g.Clear(CanvasColor);
            }
            else
            {
                g = Graphics.FromImage(frameBuffer);
            }

            f(n1, n2, g);

            canvas.Image = frameBuffer;
        }

        #endregion

        private void initCanvas(Graphics g)
        {
            
        }

        private void renderNodes(Graphics g)
        {
            foreach (Node n1 in nodes)
            {
                foreach (Node n2 in nodes)
                {
                    drawEdge(n1, n2, g);
                }
            }
        }

        #region Edge Rendering Parameters

        int penAlpha;
        int penWidth;

        const int maxAlpha = 100;
        const int maxWidth = 3;

        const int dropDistance = 330;

        double alphaGrad;
        double widthGrad;

        double NodeSeparation;

        private void initEdgeVals()
        {
            alphaGrad = (double) maxAlpha / dropDistance;
            widthGrad = (double) maxWidth / dropDistance;
        }

        #endregion

        private void drawEdge(Node n1, Node n2, Graphics g)
        {
            if (n1 == n2)
            {
                return;
            }

            NodeSeparation = getDistance(n1.getPos(), n2.getPos());
            penAlpha = Math.Max(0, maxAlpha - (int)(alphaGrad * NodeSeparation));
            penWidth = Math.Max(0, maxWidth - (int)(widthGrad * NodeSeparation));

            if (penAlpha > 0 && penWidth > 0)
            {
                g.DrawLine(new Pen(Color.FromArgb(penAlpha, LineColor), penWidth), n1.getPos(), n2.getPos());
            }
        }

        private int getDistance(Point p1, Point p2)
        {
            return (int)Math.Sqrt(Math.Pow((p1.X - p2.X), 2) + Math.Pow((p1.Y - p2.Y), 2));
        }

        private Point getMidCanvas()
        {
            return new Point(canvas.Width / 2, canvas.Height / 2);
        }

        private void addNewNode(Node n)
        {
            nodes.Add(n);
            renderWrapper(n, renderNode);

        }

        private void renderNode(Node n1, Graphics g)
        {
            foreach (Node n2 in nodes)
            {
                drawEdge(n1, n2, g);
            }
        }

        #endregion

        #region Form Events

        private void FormResize(object sender, EventArgs e)
        {
            foreach (Node n in nodes)
            {
                n.setContainer(canvas.Size);
            }
            renderWrapper(renderNodes, flush:true);
        }

        private void FormClick(object sender, EventArgs e)
        {
            MouseEventArgs em = (MouseEventArgs)e;
            if (em.Button == MouseButtons.Right)
            {
                addNewNode(new Node(em.Location, canvas.Size));
            }
            
        }

        #endregion

    }

    public class Node
    {
        private doubleVector location;
        private Size container;

        public Node(Point Location, Size containerSize) : base()
        {
            setContainer(containerSize);

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

