﻿using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using OpenTK;
using OpenTK.Graphics.OpenGL4;
using X3D.Core;
using X3D.Core.Shading;

namespace X3D
{
    public partial class Text
    {
        private string _combined_text;
        private Vector3 _start_position;
        private FontStyle fontStyle;
        private Dictionary<string, ImageTexture> stringTextures = new Dictionary<string, ImageTexture>();
        private List<Vertex> verts = new List<Vertex>();

        public Text(Vector3 position, string text)
        {
            @string = text;

            _strings = new List<string>();
            _strings.Add(text);
            _protoInstance = new ProtoInstance();
            _screenFontStyle = new ScreenFontStyle();
            _fontStyle = new FontStyle();
            _maxExtent = 0F;
            _solid = false;
            containerField = "geometry";
        }

        private int Width { get; set; }
        private int Height { get; set; }
        private Vector3 Offset { get; set; }
        private Font Font { get; set; }
        private System.Drawing.Color ForeColor { get; set; }
        private System.Drawing.Color BackColor { get; set; }

        public override void CollectGeometry(
            RenderingContext rc,
            out GeometryHandle handle,
            out BoundingBox bbox,
            out bool coloring,
            out bool texturing)
        {
            handle = GeometryHandle.Zero;
            bbox = BoundingBox.Zero;
            coloring = false;
            texturing = true;

            var line = 0;
            float newLinePositionY;
            var geometry = new List<Vertex>();
            Vertex v;

            fontStyle = FontStyle;

            var z = 0f;
            var h = 0.5f;

            var combined = string.Empty;

            if (_strings.Count == 0) _strings.Add("Abc");

            ForeColor = System.Drawing.Color.FromArgb(255, 255, 255, 255);
            ForeColor = System.Drawing.Color.Cyan;
            BackColor = System.Drawing.Color.FromArgb(0, 0, 0, 0);

            _start_position = new Vector3(0f, 0.5f, z);

            string family;
            float size;

            foreach (var text in _strings)
            {
                family = FontStyle.Family.First();
                family = string.IsNullOrEmpty(family) ? "Times New Roman" : family;
                size = FontStyle.size * 10;
                Font = new Font(family, size);

                newLinePositionY = line * 0.5f;

                var scale = 0.01f;

                if (line == 0)
                {
                    v = new Vertex
                    {
                        Position = new Vector3(0f, 0f + newLinePositionY, z) * scale,
                        TexCoord = new Vector2(0f, 1f)
                    };
                    geometry.Add(v);

                    v = new Vertex
                    {
                        Position = new Vector3(1.0f, 0f + newLinePositionY, z) * scale,
                        TexCoord = new Vector2(1f, 1f)
                    };
                    geometry.Add(v);

                    v = new Vertex
                    {
                        Position = new Vector3(1.0f, h + newLinePositionY, z) * scale,
                        TexCoord = new Vector2(1f, 0f)
                    };
                    geometry.Add(v);

                    v = new Vertex
                    {
                        Position = new Vector3(0f, h + newLinePositionY, z) * scale,
                        TexCoord = new Vector2(0f, 0f)
                    };

                    geometry.Add(v);
                }

                combined += text + (line < _strings.Count ? "\n" : "");

                line++;
            }

            _combined_text = combined;

            UpdateString(combined, out bbox);


            Buffering.BufferShaderGeometry(geometry, out handle.vbo4, out handle.NumVerticies4);
        }

        #region Private Methods

        private void UpdateString(string text, out BoundingBox bbox)
        {
            Brush b;
            PointF p;
            Bitmap bmp;

            bbox = BoundingBox.CalculateBoundingBox(text, Font);

            bmp = new Bitmap((int)bbox.Width, (int)bbox.Height);

            bmp.MakeTransparent(BackColor);

            //if(FontStyle.Justify.FirstOrDefault() == "MIDDLE")
            //{
            //p = new PointF((float)Offset.X + (boundingbox.Width / 8.0f), (float)Offset.Y);
            //}
            //else
            //{
            p = new PointF(Offset.X, Offset.Y);
            //}


            b = new SolidBrush(ForeColor);

            using (var g2D = Graphics.FromImage(bmp))
            {
                //g2D.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAlias;
                g2D.Clear(BackColor);
                g2D.DrawString(text, Font, b, p, StringFormat.GenericDefault);
            }

            bmp.RotateFlip(RotateFlipType.RotateNoneFlipX);

            var texture =
                ImageTexture.CreateTextureFromImage(bmp, new Rectangle(0, 0, (int)bbox.Width, (int)bbox.Height));

            if (stringTextures.ContainsKey(text))
            {
                stringTextures[text].FreeTexture();
                stringTextures[text].Dispose();

                stringTextures[text] = texture;
            }
            else
            {
                stringTextures.Add(text, texture);
            }
        }

        #endregion


        #region Rendering Methods

        public void BindTextures(RenderingContext rc)
        {
            var stringTexture = stringTextures.First();

            stringTexture.Value.Render(rc);
            GL.BindTexture(TextureTarget.Texture2D, stringTexture.Value.Index);
        }

        public void UnbindTextures(RenderingContext rc)
        {
            var stringTexture = stringTextures.First();
            stringTexture.Value.Unbind();
        }

        #endregion
    }
}