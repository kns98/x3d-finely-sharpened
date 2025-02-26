﻿using System.ComponentModel;
using System.Threading;
using GraphDebugger.OpenGL;
using X3D.Engine;

namespace X3D
{
    public class X3DGraphDebugger
    {
        public static bool hasLoaded;
        private static AutoResetEvent closureEvent;
        private static BackgroundWorker worker;
        private static GraphView view;

        public static void Display(SceneGraph graph)
        {
            if (!hasLoaded)
            {
                view = GraphView.CreateView(graph, out closureEvent, out worker);
                //OpenGL.UniformVariableTestProgram.View_Init();

                hasLoaded = true;
            }
        }

        public static void Hide()
        {
            if (hasLoaded)
            {
                if (!worker.CancellationPending)
                    worker.CancelAsync();

                closureEvent.Set();
            }
        }

        public static void UpdateSceneGraph(SceneGraph graph)
        {
            if (hasLoaded)
            {
                if (view == null)
                {
                    hasLoaded = false;
                    Display(graph);
                }
                else
                {
                    view.Graph = graph;

                    view.RebuildLayout();
                }
            }
        }
    }
}