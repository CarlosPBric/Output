﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;
using XOutput.Input.XInput;

namespace XOutput.UI.Converters
{
    /// <summary>
    /// Translates a text.
    /// </summary>
    public class ColorConverter : IMultiValueConverter
    {
        protected Dictionary<XInputTypes, Brush> foregroundColors = new Dictionary<XInputTypes, Brush>();
        protected Dictionary<XInputTypes, Brush> backgroundColors = new Dictionary<XInputTypes, Brush>();
        protected Dictionary<XInputTypes, Brush> labelColors = new Dictionary<XInputTypes, Brush>();

        protected static readonly Brush dpad = CreateData(153, 153, 153);
        protected static readonly Brush dpadBack = CreateData(134, 134, 134);

        public ColorConverter()
        {
            foregroundColors.Add(XInputTypes.A, CreateData(53, 217, 0));
            backgroundColors.Add(XInputTypes.A, CreateData(36, 149, 0));
            labelColors.Add(XInputTypes.A, CreateData(173, 255, 146));
            foregroundColors.Add(XInputTypes.B, CreateData(255, 19, 3));
            backgroundColors.Add(XInputTypes.B, CreateData(171, 11, 0));
            labelColors.Add(XInputTypes.B, CreateData(255, 161, 155));
            foregroundColors.Add(XInputTypes.X, CreateData(14, 82, 255));
            backgroundColors.Add(XInputTypes.X, CreateData(0, 50, 176));
            labelColors.Add(XInputTypes.X, CreateData(133, 167, 255));
            foregroundColors.Add(XInputTypes.Y, CreateData(255, 232, 35));
            backgroundColors.Add(XInputTypes.Y, CreateData(217, 195, 0));
            labelColors.Add(XInputTypes.Y, CreateData(255, 249, 193));
            foregroundColors.Add(XInputTypes.L1, CreateData(255, 248, 248));
            backgroundColors.Add(XInputTypes.L1, CreateData(241, 241, 241));
            labelColors.Add(XInputTypes.L1, CreateData(187, 187, 187));
            foregroundColors.Add(XInputTypes.R1, CreateData(255, 248, 248));
            backgroundColors.Add(XInputTypes.R1, CreateData(241, 241, 241));
            labelColors.Add(XInputTypes.R1, CreateData(187, 187, 187));
            foregroundColors.Add(XInputTypes.L2, CreateData(248, 248, 248));
            backgroundColors.Add(XInputTypes.L2, CreateData(187, 187, 187));
            labelColors.Add(XInputTypes.L2, CreateData(187, 187, 187));
            foregroundColors.Add(XInputTypes.R2, CreateData(248, 248, 248));
            backgroundColors.Add(XInputTypes.R2, CreateData(187, 187, 187));
            labelColors.Add(XInputTypes.R2, CreateData(187, 187, 187));
            foregroundColors.Add(XInputTypes.L3, CreateData(153, 153, 153));
            backgroundColors.Add(XInputTypes.L3, CreateData(134, 134, 134));
            foregroundColors.Add(XInputTypes.R3, CreateData(153, 153, 153));
            backgroundColors.Add(XInputTypes.R3, CreateData(134, 134, 134));
            foregroundColors.Add(XInputTypes.Start, CreateData(241, 241, 241));
            backgroundColors.Add(XInputTypes.Start, CreateData(217, 217, 217));
            foregroundColors.Add(XInputTypes.Back, CreateData(241, 241, 241));
            backgroundColors.Add(XInputTypes.Back, CreateData(217, 217, 217));
            foregroundColors.Add(XInputTypes.Home, CreateData(202, 202, 202));
            backgroundColors.Add(XInputTypes.Home, CreateData(119, 236, 0));


        }

        /// <summary>
        /// Translates a text.
        /// </summary>
        /// <param name="values">Ignored</param>
        /// <param name="targetType">Ignored</param>
        /// <param name="parameter">Text key</param>
        /// <param name="culture">Ignored</param>
        /// <returns></returns>
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            XInputTypes? activeType = values[0] as XInputTypes?;
            bool? highlight = values[1] as bool?;
            var parameters = (parameter as string).Split('|');
            bool back = parameters.Length > 1 && parameters[1] == "back";
            bool label = parameters.Length > 1 && parameters[1] == "label";
            if (parameters[0] == "DPAD")
            {
                if (back)
                {
                    if (highlight == true && XInputHelper.Instance.IsDPad(activeType.Value))
                        return new SolidColorBrush(Color.FromRgb(128, 0, 0));
                    return dpadBack;
                }
                else
                {
                    if (highlight == true && XInputHelper.Instance.IsDPad(activeType.Value))
                        return new SolidColorBrush(Color.FromRgb(255, 0, 0));
                    return dpad;
                }
            }
            else
            {
                var currentType = (XInputTypes)Enum.Parse(typeof(XInputTypes), parameters[0]);
                if (back)
                {
                    if (highlight == true && currentType == activeType)
                        return new SolidColorBrush(Color.FromRgb(128, 0, 0));
                    else if (backgroundColors.ContainsKey(currentType))
                        return backgroundColors[currentType];
                }
                else if (label)
                {
                    if (highlight == true && currentType == activeType)
                        return new SolidColorBrush(Color.FromRgb(255, 255, 255));
                    else if (labelColors.ContainsKey(currentType))
                        return labelColors[currentType];
                }
                else
                {
                    if (highlight == true && currentType == activeType)
                        return new SolidColorBrush(Color.FromRgb(255, 0, 0));
                    else if (foregroundColors.ContainsKey(currentType))
                        return foregroundColors[currentType];
                }
            }
            return new SolidColorBrush(Colors.Black);
        }

        /// <summary>
        /// Intentionally not implemented.
        /// </summary>
        /// <param name="value">Ignored</param>
        /// <param name="targetTypes">Ignored</param>
        /// <param name="parameter">Ignored</param>
        /// <param name="culture">Ignored</param>
        /// <returns></returns>
        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }

        protected static Brush CreateData(byte r, byte g, byte b)
        {
            return new SolidColorBrush(Color.FromRgb(r, g, b));
        }
    }
}