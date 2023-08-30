﻿using System;
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
        }

        private void Main_Load(object sender, EventArgs e)
        {
            initEdgeVals();
            renderAll();
        }

        #endregion

        #region Updates and Rendering

        #region Render Properties

        static readonly Color BackdropColor = Color.Black;
        static readonly Color EdgeColor = Color.Cyan;
        static readonly Color NodeColor = Color.DarkSlateGray;

        static readonly int NodeRadius = 20;

        Bitmap frameBuffer = new Bitmap(1, 1);

        Bitmap[] layers = new Bitmap[] { new Bitmap(1, 1), new Bitmap(1, 1), new Bitmap(1, 1) };

        #endregion

        #region Render Wrappers

        private void renderWrapper(renderParams p, Action<renderParams, Graphics> f, bool resize = false)
        {
            if (resize)
            {
                frameBuffer = new Bitmap(canvas.Width, canvas.Height);
                layers[(int)p.layer] = new Bitmap(canvas.Width, canvas.Height);
            }

            Graphics g = Graphics.FromImage(frameBuffer);

            f(p, Graphics.FromImage(layers[(int)p.layer]));

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

        //const int maxAlpha = 100;
        const int maxAlpha = 255;
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

        private void drawNode(renderParams p, Graphics g)
        {
            Point pos = p.mainNode.getPos();
            int left = pos.X - NodeRadius;
            int top = pos.Y - NodeRadius;
            int diameter = 2 * NodeRadius;

            g.FillEllipse(new Pen(p.color).Brush, left, top, diameter, diameter);
        }

        private void drawEdge(renderParams p, Graphics g)
        {
            if (p.mainNode == p.secondaryNode)
            {
                return;
            }

            NodeSeparation = getDistance(p.mainNode.getPos(), p.secondaryNode.getPos());
            penAlpha = Math.Max(0, maxAlpha - (int)(alphaGrad * NodeSeparation));
            penWidth = Math.Max(0, maxWidth - (int)(widthGrad * NodeSeparation));

            if (penAlpha > 0 && penWidth > 0)
            {
                g.DrawLine(new Pen(Color.FromArgb(penAlpha, p.color), penWidth), p.mainNode.getPos(), p.secondaryNode.getPos());
            }
        }

        private void drawNodeEdges(renderParams p, Graphics g)
        {
            foreach (Node n in nodes)
            {
                drawEdge(new renderParams(p.layer, p.color, p.mainNode, n), g);
            }
        }

        private void addNewNode(Node n)
        {
            nodes.Add(n);
            renderWrapper(new renderParams(Layer.Edges, EdgeColor, n), drawNodeEdges);
            renderWrapper(new renderParams(Layer.Nodes, NodeColor, n), drawNode);
        }

        #region Total Renders

        private void renderBackdrop(renderParams p, Graphics g)
        {
            g.Clear(p.color);
        }

        private void renderAllEdges(renderParams p, Graphics g)
        {
            foreach (Node n1 in nodes)
            {
                foreach (Node n2 in nodes)
                {
                    drawEdge(new renderParams(p.layer, p.color, n1, n2), g);
                }
            }
        }

        private void renderAllNodes(renderParams p, Graphics g)
        {
            foreach (Node n in nodes)
            {
                drawNode(new renderParams(p.layer, p.color, n), g);
            }
        }

        private void renderAll()
        {
            renderWrapper(new renderParams(Layer.Backdrop, BackdropColor), renderBackdrop, resize: true);
            renderWrapper(new renderParams(Layer.Edges, EdgeColor), renderAllEdges, resize: true);
            renderWrapper(new renderParams(Layer.Nodes, NodeColor), renderAllNodes, resize: true);
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
        readonly static Color SelectedEdgeColor = Color.Red;

        private void selectNode(Node n)
        {
            renderWrapper(new renderParams(Layer.Nodes, SelectedNodeColor, n), drawNode);
            renderWrapper(new renderParams(Layer.Edges, SelectedEdgeColor, n), drawNodeEdges);
        }

        private void deselectNode(Node n)
        {
            renderWrapper(new renderParams(Layer.Nodes, NodeColor, n), drawNode);
            renderWrapper(new renderParams(Layer.Edges, EdgeColor, n), drawNodeEdges);
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
                            selectNode(n);
                            break;
                        case State.None:
                            deselectNode(n);
                            break;
                    }
                }
            }
        }

        #endregion

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

        #region Structs

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

        public struct renderParams
        {
            public Node mainNode;
            public Node secondaryNode;
            public Color color;
            public Layer layer;

            public renderParams(Layer l, Color c, Node m = null, Node s = null)
            {
                mainNode = m;
                secondaryNode = s;
                color = c;
                layer = l;
            }
        }

        #endregion

        #region Enums

        public enum Layer
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

        #endregion
    }
}

