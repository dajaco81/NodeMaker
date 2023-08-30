using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Reflection.Emit;
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
            renderAll();
        }

        #endregion

        #region Updates and Rendering

        #region Render Properties

        static readonly Color BackdropColor = Color.Black;
        static readonly Color LineColor = Color.Cyan;
        static readonly Color NodeColor = Color.DarkSlateGray;

        static readonly int NodeRadius = 20;

        Bitmap frameBuffer = new Bitmap(1, 1);

        Bitmap[] layers = new Bitmap[] { new Bitmap(1, 1), new Bitmap(1, 1), new Bitmap(1, 1) };

        #endregion

        #region Render Wrappers

        private void renderWrapper(Action<Graphics> f, int layer, bool resize = false)
        {
            if (resize)
            {
                frameBuffer = new Bitmap(canvas.Width, canvas.Height);
                layers[layer] = new Bitmap(canvas.Width, canvas.Height);
            }

            Graphics g = Graphics.FromImage(frameBuffer);

            f(Graphics.FromImage(layers[layer]));

            foreach (Bitmap l in layers)
            {
                g.DrawImage(l, 0, 0);
            }

            canvas.Image = frameBuffer;
        }

        private void renderWrapper(Node n, Action<Node, Graphics> f, int layer, bool resize = false)
        {
            if (resize)
            {
                frameBuffer = new Bitmap(canvas.Width, canvas.Height);
                layers[layer] = new Bitmap(canvas.Width, canvas.Height);
            }

            Graphics g = Graphics.FromImage(frameBuffer);

            f(n, Graphics.FromImage(layers[layer]));

            foreach (Bitmap l in layers)
            {
                g.DrawImage(l, 0, 0);
            }

            canvas.Image = frameBuffer;
        }

        private void renderWrapper(Node n1, Node n2, Action<Node, Node, Graphics> f, int layer, bool resize = false)
        {
            if (resize)
            {
                frameBuffer = new Bitmap(canvas.Width, canvas.Height);
                layers[layer] = new Bitmap(canvas.Width, canvas.Height);
            }

            Graphics g = Graphics.FromImage(frameBuffer);

            f(n1, n2, Graphics.FromImage(layers[layer]));

            foreach (Bitmap l in layers)
            {
                g.DrawImage(l, 0, 0);
            }

            canvas.Image = frameBuffer;
        }

        #endregion

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
            alphaGrad = (double)maxAlpha / dropDistance;
            widthGrad = (double)maxWidth / dropDistance;
        }

        #endregion

        #region Render Procedures

        private void drawNodeAtLocation(Point pos, Color c, Graphics g)
        {
            int left = pos.X - NodeRadius;
            int top = pos.Y - NodeRadius;
            int diameter = 2 * NodeRadius;

            g.FillEllipse(new Pen(c).Brush, left, top, diameter, diameter);
        }

        private void drawNode(Node n, Graphics g)
        {
            drawNodeAtLocation(n.getPos(), NodeColor, g);
        }

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

        private void drawNodeEdges(Node n, Graphics g)
        {
            foreach (Node n2 in nodes)
            {
                drawEdge(n, n2, g);
            }
        }

        private void renderNewNode(Node n)
        {
            renderWrapper(n, drawNodeEdges, (int)Layers.Edges);
            renderWrapper(n, drawNode, (int)Layers.Nodes);
        }

        private void addNewNode(Node n)
        {
            nodes.Add(n);
            renderNewNode(n);
        }

        #region Total Renders

        private void renderBackdrop(Graphics g)
        {
            g.Clear(BackdropColor);
        }

        private void renderAllEdges(Graphics g)
        {
            foreach (Node n1 in nodes)
            {
                foreach (Node n2 in nodes)
                {
                    drawEdge(n1, n2, g);
                }
            }
        }

        private void renderAllNodes(Graphics g)
        {
            foreach (Node n in nodes)
            {
                drawNode(n, g);
            }
        }

        private void renderAll()
        {
            renderWrapper(renderBackdrop, (int)Layers.Backdrop, resize: true);
            renderWrapper(renderAllEdges, (int)Layers.Edges, resize: true);
            renderWrapper(renderAllNodes, (int)Layers.Nodes, resize: true);
        }

        #endregion

        #endregion

        #endregion

        #region Calculations
        private int getDistance(Point p1, Point p2)
        {
            return (int)Math.Sqrt(Math.Pow((p1.X - p2.X), 2) + Math.Pow((p1.Y - p2.Y), 2));
        }

        #endregion

        #region Form Events

        private void FormResize(object sender, EventArgs e)
        {
            foreach (Node n in nodes)
            {
                n.setContainer(canvas.Size);
            }
            renderAll();
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

        #region Node Selection

        readonly static Color SelectedNodeColor = Color.Green;

        private void selectNode(Node n, Graphics g)
        {
            drawNodeAtLocation(n.getPos(), SelectedNodeColor, g);
        }

        private void deselectNode(Node n, Graphics g)
        {
            drawNode(n, g);
        }

        private void canvas_MouseMove(object sender, MouseEventArgs e)
        {
            foreach (Node n in nodes)
            {
                if (getDistance(e.Location, n.getPos()) <= NodeRadius)
                {
                    n.state = State.Hover;
                }
                else
                {
                    n.state = State.None;
                }

                if (n.stateChanged())
                {
                    switch (n.state) 
                    {
                        case State.Hover:
                            renderWrapper(n, selectNode, (int)Layers.Nodes);
                            break;
                        case State.None:
                            renderWrapper(n, deselectNode, (int)Layers.Nodes);
                            break;
                    }
                }
            }
        }

        #endregion

        public enum Layers
        {
            Backdrop,
            Edges,
            Nodes
        }

        public enum State
        {
            None,
            Hover
        }

        public class Node
        {
            private doubleVector location;
            private Size container;

            public State state;
            public State oldState;

            public Node(Point Location, Size containerSize) : base()
            {
                setContainer(containerSize);
                setPos(Location);
                state = State.None;
                oldState = State.None;
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

            public bool stateChanged()
            {
                if (state != oldState)
                {
                    oldState = state;
                    return true;
                }
                return false;
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
}

