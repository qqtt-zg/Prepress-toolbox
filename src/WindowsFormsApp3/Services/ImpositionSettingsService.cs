using System;
using System.Globalization;
using WindowsFormsApp3.Models;
using WindowsFormsApp3.Utils;

namespace WindowsFormsApp3.Services
{
    /// <summary>默认参数的读写边界；临时编辑不调用保存。</summary>
    public class ImpositionSettingsService
    {
        public virtual TemporaryImpositionParameters Load()
        {
            float Number(string key, float fallback) => Parse(AppSettings.Get(key), fallback);
            var p = new TemporaryImpositionParameters
            {
                Enabled = AppSettings.GetValue<bool>("Imposition_Enabled"),
                PaperWidth = Number("Imposition_PaperWidth", 320), PaperHeight = Number("Imposition_PaperHeight", 464),
                FixedWidth = Number("Imposition_FixedWidth", 320), MinLength = Number("Imposition_MinLength", 420),
                MarginTop = Number("Imposition_MarginTop", 10), MarginBottom = Number("Imposition_MarginBottom", 10),
                MarginLeft = Number("Imposition_MarginLeft", 10), MarginRight = Number("Imposition_MarginRight", 10),
                RollMarginTop = Number("Imposition_RollMarginTop", 10), RollMarginBottom = Number("Imposition_RollMarginBottom", 10),
                RollMarginLeft = Number("Imposition_RollMarginLeft", 10), RollMarginRight = Number("Imposition_RollMarginRight", 10),
                SpacingX = Number("Imposition_SpacingX", 0), SpacingY = Number("Imposition_SpacingY", 0),
                RequestedRows = (int)Number("Imposition_Rows", 0), RequestedColumns = (int)Number("Imposition_Columns", 0),
                FlatSheetRotationMode = Rotation("Imposition_FlatRotationMode"),
                RollMaterialRotationMode = Rotation("Imposition_RollRotationMode")
            };
            p.Strategy = p.RequestedRows > 0 || p.RequestedColumns > 0
                ? ImpositionLayoutStrategy.FixedGrid : ImpositionLayoutStrategy.Automatic;
            return p;
        }

        private static RollRotationMode Rotation(string key)
        {
            return Enum.TryParse(Convert.ToString(AppSettings.Get(key)), out RollRotationMode mode) &&
                Enum.IsDefined(typeof(RollRotationMode), mode) ? mode : RollRotationMode.Auto;
        }

        private static float Parse(object value, float fallback)
        {
            string text = Convert.ToString(value, CultureInfo.InvariantCulture);
            return float.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out float number) &&
                !float.IsNaN(number) && !float.IsInfinity(number) ? number : fallback;
        }

        public virtual void Save(TemporaryImpositionParameters parameters)
        {
            var p = parameters.EffectiveLayout();
            void Number(string key, float value) => AppSettings.Set(key, value.ToString("0.###", CultureInfo.InvariantCulture));
            AppSettings.Set("Imposition_Enabled", p.Enabled);
            Number("Imposition_PaperWidth", p.PaperWidth); Number("Imposition_PaperHeight", p.PaperHeight);
            Number("Imposition_FixedWidth", p.FixedWidth); Number("Imposition_MinLength", p.MinLength);
            Number("Imposition_MarginTop", p.MarginTop); Number("Imposition_MarginBottom", p.MarginBottom);
            Number("Imposition_MarginLeft", p.MarginLeft); Number("Imposition_MarginRight", p.MarginRight);
            Number("Imposition_RollMarginTop", p.RollMarginTop); Number("Imposition_RollMarginBottom", p.RollMarginBottom);
            Number("Imposition_RollMarginLeft", p.RollMarginLeft); Number("Imposition_RollMarginRight", p.RollMarginRight);
            Number("Imposition_SpacingX", p.SpacingX); Number("Imposition_SpacingY", p.SpacingY);
            // 自动必须显式保存 0，不能跳过空输入而继续使用旧固定值。
            Number("Imposition_Rows", p.RequestedRows); Number("Imposition_Columns", p.RequestedColumns);
            AppSettings.Set("Imposition_FlatRotationMode", (int)(p.FlatSheetRotationMode ?? RollRotationMode.Auto));
            AppSettings.Set("Imposition_RollRotationMode", (int)(p.RollMaterialRotationMode ?? RollRotationMode.Auto));
            AppSettings.Save();
        }
    }
}
