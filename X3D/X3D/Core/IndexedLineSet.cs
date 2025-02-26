﻿using System.Collections.Generic;
using System.Linq;
using OpenTK;
using OpenTK.Graphics.OpenGL4;
using X3D.Core;
using X3D.Core.Shading;
using X3D.Parser;

namespace X3D
{
    public partial class IndexedLineSet
    {
        #region Public Static Methods

        public static IndexedLineSet CreateFromVertexSet(List<Vertex> verticies)
        {
            IndexedLineSet ils;
            Coordinate coord;
            Vector3[] points;
            int[] indicies;
            int i;

            ils = new IndexedLineSet();
            coord = new Coordinate();

            points = verticies.Select(v => v.Position).ToArray();

            i = -1;
            indicies = verticies.Select(v => ++i).ToArray();

            coord.point = X3DTypeConverters.ToString(points);

            ils.coordIndex = X3DTypeConverters.ToString(indicies);
            ils.Children.Add(coord);
            ils.PrimativeType = PrimitiveType.Lines;

            return ils;
        }

        #endregion

        #region Public Methods

        public override void CollectGeometry(
            RenderingContext rc,
            out GeometryHandle handle,
            out BoundingBox bbox,
            out bool coloring,
            out bool texturing)
        {
            bbox = BoundingBox.Zero;

            // INTERLEAVE
            _pack = PackedGeometry.Pack(this);

            coloring = _pack.Coloring;
            texturing = _pack.Texturing;
            bbox = _pack.bbox;

            // BUFFER GEOMETRY
            handle = _pack.CreateHandle();
        }

        #endregion

        #region Fields

        internal PackedGeometry _pack;

        public PrimitiveType PrimativeType = PrimitiveType.LineLoop;

        #endregion
    }
}