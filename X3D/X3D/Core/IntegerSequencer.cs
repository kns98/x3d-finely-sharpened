﻿using System.Collections.Generic;
using System.Xml.Serialization;
using X3D.Parser;

namespace X3D
{
    /// <summary>
    ///     http://www.web3d.org/documents/specifications/19775-1/V3.3/Part01/components/utils.html#IntegerSequencer
    /// </summary>
    public partial class IntegerSequencer
    {
        #region Private Fields

        private float _set_fraction;
        private int _value_changed;
        private int[] _keyValues;
        private Dictionary<float, int> map = new Dictionary<float, int>();

        #endregion

        #region Public Fields

        [XmlIgnore]
        public float set_fraction
        {
            private get { return _set_fraction; }
            set
            {
                _set_fraction = value;

                var percent = value * 100;

                if (map.ContainsKey(percent)) value_changed = map[percent];
            }
        }

        [XmlIgnore]
        public int value_changed
        {
            get => _value_changed;
            private set => _value_changed = value;
        }

        #endregion

        #region Rendering Methods

        public override void Load()
        {
            base.Load();

            int i;

            _keyValues = X3DTypeConverters.Integers(keyValue);

            // OPTIMISE
            for (i = 0; i < Keys.Length; i++)
                if (i >= _keyValues.Length)
                    map[Keys[i]] = 0;
                else
                    map[Keys[i]] = _keyValues[i];
        }

        public override void PreRenderOnce(RenderingContext rc)
        {
            base.PreRenderOnce(rc);
        }

        public override void Render(RenderingContext rc)
        {
            base.Render(rc);
        }

        #endregion
    }
}