using System.Drawing;
using System.Windows.Forms;

namespace OSRCGG
{
    public partial class AcksToolkitForm
    {
        private void ApplyUnifiedInterfaceStyle()
        {
            // Все вкладки используют одну гарнитуру и размер. Жирность сохраняет
            // различие между обычным текстом, заголовками и командами.
            UiTheme.ApplyUniformFonts(this);
            UiTheme.ApplyThemeColors(this);

            Button[] positiveButtons =
            {
                btnApply, btnExport, btnGenerate, btnGeneratedSettlementSave, btnApplyLandMod,
                btnGeneratorLoadSettlement, btnCalcRoute, btnExportRoute, btnImportMarketA,
                btnImportPartner, btnTradeUseMarketASettlement, btnTradeUsePartnerSettlement,
                btnCharacterNew, btnCharacterSave, btnCharacterImport, btnCharacterExport,
                btnCharacterRandomPlayer, btnCharacterRandomNpc, btnCharacterRollAttributes,
                btnCharacterRandomProficiencies, btnMapNew, btnMapSave, btnMapSaveSettlement,
                btnMapCreateEmptySettlement, btnMapExportSettlement, btnMapImportSettlement,
                btnMapApplyDemands, btnMapCalculateTrade, btnMapExportExcel, btnMapImportExcel,
                btnMapDomainNew, btnMapDomainEdit, btnMapDomainAddHex, btnMapDomainRecalculate
            };

            Button[] negativeButtons =
            {
                btnGeneratorDeleteSettlement, btnCharacterDelete, btnMapDelete,
                btnMapDomainDelete, btnMapDomainRemoveHex
            };

            foreach (Button button in positiveButtons)
            {
                UiTheme.StylePositiveButton(button);
            }

            foreach (Button button in negativeButtons)
            {
                UiTheme.StyleNegativeButton(button);
            }

            // Служебные команды сохраняют отдельные роли, но используют общий шрифт.
            StyleNeutralButton(btnLang);
            StyleNeutralButton(btnMusicToggle);
            UiTheme.StyleCommandButton(btnClose, UiTheme.NegativeButtonColor);
            RestoreMapPickerButtonStyle();
        }

        private void StyleNeutralButton(Button button)
        {
            if (button == null) return;
            button.Font = UiTheme.CreateFont(FontStyle.Bold);
            button.BackColor = UiTheme.NeutralButtonColor;
            button.ForeColor = UiTheme.TextColor;
            button.FlatStyle = FlatStyle.Standard;
            button.UseVisualStyleBackColor = false;
        }
    }
}
