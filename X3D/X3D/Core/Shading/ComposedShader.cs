﻿//TODO: standardize shader variables to be same as x3dom http://doc.x3dom.org/tutorials/lighting/customShader/
// this way there shouldn't be any trouble porting ShaderParts

using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Serialization;
using OpenTK;
using OpenTK.Graphics.OpenGL4;
using X3D.Core;
using X3D.Core.Shading.DefaultUniforms;
using X3D.Parser;

namespace X3D
{
    public enum VertexAttribType
    {
        Position,
        Normal,
        TextureCoord,
        Color
    }

    public partial class ComposedShader
    {
        [XmlIgnore] public bool HasErrors;

        [XmlIgnore] public int ShaderHandle;

        [XmlIgnore] public List<ShaderPart> ShaderParts = new List<ShaderPart>();

        private readonly ShaderUniformsPNCT uniforms = new ShaderUniformsPNCT();

        public ComposedShader()
        {
            containerField = "shaders";
        }

        [XmlIgnore] public List<field> Fields { get; set; }

        [XmlIgnore] public bool Linked { get; internal set; }

        [XmlIgnore]
        public bool IsTessellator
        {
            get
            {
                var sps = ShaderParts.Any(s => s.type == shaderPartTypeValues.TESS_CONTROL
                                               || s.type == shaderPartTypeValues.TESS_EVAL
                                               || s.type == shaderPartTypeValues.GEOMETRY
                );

                return sps;
            }
        }

        /// <summary>
        ///     If the shader is a built in system shader
        /// </summary>
        [XmlIgnore]
        public bool IsBuiltIn { get; internal set; }

        public override void Load()
        {
            Fields = new List<field>();
            ShaderParts = new List<ShaderPart>();
            base.Load();
        }

        public override void PostDescendantDeserialization()
        {
            base.PostDescendantDeserialization();

            GetParent<Shape>().IncludeComposedShader(this);
        }


        public ComposedShader Use()
        {
            if (!HasErrors) {
                try
                {

                    GL.UseProgram(ShaderHandle);
                }
                catch(Exception ex)
                {
                    Console.WriteLine(ex.StackTrace); 

                }
            }
            return this;
        }

        public void Unbind()
        {
            if (!HasErrors) GL.UseProgram(0);
        }

        public void SetSampler(int sampler)
        {
            GL.Uniform1(uniforms.sampler, sampler);
        }

        public void Deactivate()
        {
            GL.UseProgram(0);
        }

        public void Link()
        {
            Console.WriteLine("ComposedShader {0}", language);

            if (language == "GLSL")
            {
                ShaderHandle = GL.CreateProgram();

                foreach (var part in ShaderParts) ShaderCompiler.ApplyShaderPart(ShaderHandle, part);

                GL.LinkProgram(ShaderHandle);
                var err = GL.GetProgramInfoLog(ShaderHandle).Trim();

                if (!string.IsNullOrEmpty(err))
                {
                    Console.WriteLine(err);

                    if (err.ToLower().Contains("error")) HasErrors = true;
                }


                if (GL.GetError() != ErrorCode.NoError)
                {
                    HasErrors = true;
                    //throw new Exception("Error Linking ComposedShader Shader Program");
                }
                else
                {
                    Linked = true;

                    Console.WriteLine("ComposedShader [linked]"); //TODO: check for more link errors

                    try
                    {
                        GL.UseProgram(ShaderHandle);
                        BindDefaultPointers();
                        GL.UseProgram(0);
                    }
                    catch
                    {
                    }
                }
            }
            else
            {
                Console.WriteLine("ComposedShader language {0} unsupported", language);
            }
        }

        internal void ApplyFieldsAsUniforms(RenderingContext rc)
        {
            var fields = Children.Where(n => n.GetType() == typeof(field)).Select(n => (field)n).ToArray();

            foreach (var f in fields)
            {
                var access = f.accessType.ToLower();

                if (access == "inputonly" || access == "inputoutput") SetFieldValueSTR(f.name, f.value, f.type);
            }
        }

        #region Buffer Data Pointer Helpers

        public void SetPointer(string name, VertexAttribType type)
        {
            //int uniformLoc = GL.GetUniformLocation(ShaderHandle, name); // can only get pointer right after Linking
            switch (type)
            {
                case VertexAttribType.Position:
                    if (uniforms.a_position != -1)
                    {
                        GL.EnableVertexAttribArray(uniforms.a_position);
                        GL.VertexAttribPointer(uniforms.a_position, 3, VertexAttribPointerType.Float, false,
                            Vertex.Stride, (IntPtr)0);
                    }

                    break;
                case VertexAttribType.TextureCoord:
                    if (uniforms.a_texcoord != -1)
                    {
                        GL.EnableVertexAttribArray(uniforms.a_texcoord);
                        GL.VertexAttribPointer(uniforms.a_texcoord, 2, VertexAttribPointerType.Float, false,
                            Vertex.Stride, (IntPtr)(Vector3.SizeInBytes + Vector3.SizeInBytes + Vector4.SizeInBytes));
                    }

                    break;
                case VertexAttribType.Normal:
                    if (uniforms.a_normal != -1)
                    {
                        GL.EnableVertexAttribArray(uniforms.a_normal);
                        GL.VertexAttribPointer(uniforms.a_normal, 3, VertexAttribPointerType.Float, false,
                            Vertex.Stride, (IntPtr)Vector3.SizeInBytes);
                    }

                    break;
                case VertexAttribType.Color:
                    if (uniforms.a_color != -1)
                    {
                        GL.EnableVertexAttribArray(uniforms.a_color);
                        GL.VertexAttribPointer(uniforms.a_color, 4, VertexAttribPointerType.Float, false, Vertex.Stride,
                            (IntPtr)(Vector3.SizeInBytes + Vector3.SizeInBytes));
                    }

                    break;
            }
        }

        public void BindDefaultPointers()
        {
            uniforms.a_position = GL.GetAttribLocation(ShaderHandle, "position");
            uniforms.a_normal = GL.GetAttribLocation(ShaderHandle, "normal");
            uniforms.a_color = GL.GetAttribLocation(ShaderHandle, "color");
            uniforms.a_texcoord = GL.GetAttribLocation(ShaderHandle, "texcoord");
        }

        #endregion

        #region Field Setter Helpers

        /// <summary>
        ///     Updates the field with a new value just in the SceneGraph.
        ///     (Changes in the field are picked up by a currrently running X3DProgrammableShaderObject)
        /// </summary>
        public void setFieldValue(string name, object value)
        {
            var field = (field)Children
                .FirstOrDefault(n => n.GetType() == typeof(field)
                                     && n.getAttribute("name").ToString() == name);

            Type type;
            object convValue;
            Type conv;

            try
            {
                type = X3DTypeConverters.X3DSimpleTypeToManagedType(field.type);
                convValue = Convert.ChangeType(value, type);
                conv = convValue.GetType();

                if (conv == typeof(int)) UpdateField(name, X3DTypeConverters.ToString((int)convValue));
                if (conv == typeof(float)) UpdateField(name, X3DTypeConverters.ToString((float)convValue));
                if (conv == typeof(Vector3)) UpdateField(name, X3DTypeConverters.ToString((Vector3)convValue));
                if (conv == typeof(Vector4)) UpdateField(name, X3DTypeConverters.ToString((Vector4)convValue));
                if (conv == typeof(Matrix3)) UpdateField(name, X3DTypeConverters.ToString((Matrix3)convValue));
                if (conv == typeof(Matrix4)) UpdateField(name, X3DTypeConverters.ToString((Matrix4)convValue));
            }
            catch
            {
                Console.WriteLine("error");
            }
        }

        public void SetFieldValueSTR(string name, string value, string x3dType)
        {
            if (HasErrors) return;

            object v;
            Type type;

            v = X3DTypeConverters.StringToX3DSimpleTypeInstance(value, x3dType, out type);

            if (type == typeof(int)) SetFieldValue(name, (int)v);
            if (type == typeof(float)) SetFieldValue(name, (float)v);
            if (type == typeof(Vector3)) SetFieldValue(name, (Vector3)v);
            if (type == typeof(Vector4)) SetFieldValue(name, (Vector4)v);
            if (type == typeof(Matrix3))
            {
                var m = (Matrix3)v;
                SetFieldValue(name, ref m);
            }

            if (type == typeof(Matrix4))
            {
                var m = (Matrix4)v;
                SetFieldValue(name, ref m);
            }
        }

        public void SetFieldValue(string name, float[] value, int sizeConstrain = -1)
        {
            if (HasErrors) return;

            var floats = value;

            if (sizeConstrain > 0)
            {
                var tmp = new float[sizeConstrain];
                value.CopyTo(tmp, 0);
                floats = tmp;
            }

            GL.Uniform1(GL.GetUniformLocation(ShaderHandle, name), floats.Length, floats);

            //UpdateField(name, X3DTypeConverters.ToString(value));
        }

        public void SetFieldValue(string name, Vector3[] value, int sizeConstrain = -1)
        {
            if (HasErrors) return;

            var vectors = new List<float>();

            foreach (var vec in value)
            {
                vectors.Add(vec.X);
                vectors.Add(vec.Y);
                vectors.Add(vec.Z);
            }

            var floats = vectors.ToArray();

            SetFieldValue(name, floats, sizeConstrain);

            //GL.Uniform3 (GL.GetUniformLocation(this.ShaderHandle, name), floats.Length, floats);

            //UpdateField(name, X3DTypeConverters.ToString(value));
        }


        public void SetFieldValue(string name, int value)
        {
            if (HasErrors) return;

            try
            {



                GL.Uniform1(GL.GetUniformLocation(ShaderHandle, name), value);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.StackTrace);
            }

            //UpdateField(name, X3DTypeConverters.ToString(value));
        }

        public void SetFieldValue(string name, float value)
        {
            if (HasErrors) return;

            try
            {
                var loc = GL.GetUniformLocation(ShaderHandle, name);
                GL.Uniform1(loc, value);
            }
            catch (Exception ex)
            {
                Console.Write(ex.StackTrace);
            }
            

            //UpdateField(name, X3DTypeConverters.ToString(value));
        }

        public void SetFieldValue(string name, Vector2 value)
        {
            if (HasErrors) return;
            try
            {
                GL.Uniform2(GL.GetUniformLocation(ShaderHandle, name), ref value);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.StackTrace);
            }
            //UpdateField(name, X3DTypeConverters.ToString(value));
        }

        public void SetFieldValue(string name, Vector3 value)
        {
            if (HasErrors) return;

            try
            {
                GL.Uniform3(GL.GetUniformLocation(ShaderHandle, name), ref value);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.StackTrace);
            }
            //UpdateField(name, X3DTypeConverters.ToString(value));
        }

        public void SetFieldValue(string name, Vector4 value)
        {
            if (HasErrors) return;

            GL.Uniform4(GL.GetUniformLocation(ShaderHandle, name), ref value);

            //UpdateField(name, X3DTypeConverters.ToString(value));
        }

        public void SetFieldValue(string name, ref Matrix3 value)
        {
            if (HasErrors) return;

            GL.UniformMatrix3(GL.GetUniformLocation(ShaderHandle, name), false, ref value);

            //TODO: convert matrix back to string and update field
            //UpdateField(name, X3DTypeConverters.ToString(value));
        }

        public void SetFieldValue(string name, ref Matrix4 value)
        {
            if (HasErrors) return;

            try
            {
                GL.UniformMatrix4(GL.GetUniformLocation(ShaderHandle, name), false, ref value);
            }
            catch (Exception ex)
            {
                Console.Write(ex.StackTrace);
            }
            //TODO: convert matrix back to string and update field
            //UpdateField(name, X3DTypeConverters.ToString(value));
        }

        public void UpdateField(string name, string value)
        {
            var fields = Children
                .Where(n => n.GetType() == typeof(field) && n.getAttribute("name").ToString() == name)
                .Select(n => (field)n).ToList();

            foreach (var f in fields) f.value = value;
        }

        #endregion
    }
}