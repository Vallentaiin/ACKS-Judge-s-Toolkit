using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace OSRCGG
{
    public sealed class HelpForm : Form
    {
        private readonly ListBox lstSections = new ListBox();
        private readonly TextBox txtContent = new TextBox();
        private readonly Dictionary<string, string> englishSections = new Dictionary<string, string>();
        private readonly Dictionary<string, string> russianSections = new Dictionary<string, string>();
        private SplitContainer split;
        private bool isEnglish;

        public HelpForm(bool isEnglish)
        {
            this.isEnglish = isEnglish;
            Text = isEnglish ? "ACKS Judge's Toolkit Help" : "Справка ACKS Judge's Toolkit";
            StartPosition = FormStartPosition.CenterParent;
            Size = new Size(780, 620);
            MinimumSize = new Size(640, 460);

            BuildSections();
            BuildUi();
            UiTheme.ApplyUniformFonts(this);
            UiTheme.ApplyThemeColors(this);
            ApplyLanguage(isEnglish);
        }

        public void ApplyLanguage(bool english)
        {
            isEnglish = english;
            Text = isEnglish ? "ACKS Judge's Toolkit Help" : "Справка ACKS Judge's Toolkit";

            string selectedKey = lstSections.SelectedItem as string;
            lstSections.Items.Clear();
            foreach (string key in CurrentSections().Keys)
            {
                lstSections.Items.Add(key);
            }

            if (!string.IsNullOrWhiteSpace(selectedKey) && lstSections.Items.Contains(selectedKey))
            {
                lstSections.SelectedItem = selectedKey;
            }
            else if (lstSections.Items.Count > 0)
            {
                lstSections.SelectedIndex = 0;
            }
        }

        private void BuildUi()
        {
            split = new SplitContainer
            {
                Dock = DockStyle.Fill,
                FixedPanel = FixedPanel.Panel1
            };
            split.Resize += (s, e) => KeepSectionListCompact();

            lstSections.Dock = DockStyle.Fill;
            lstSections.BackColor = Color.FromArgb(58, 58, 58);
            lstSections.ForeColor = Color.White;
            lstSections.BorderStyle = BorderStyle.FixedSingle;
            lstSections.SelectedIndexChanged += (s, e) => ShowSelectedSection();

            txtContent.Dock = DockStyle.Fill;
            txtContent.Multiline = true;
            txtContent.ReadOnly = true;
            txtContent.ScrollBars = ScrollBars.Vertical;
            txtContent.BackColor = Color.FromArgb(43, 43, 43);
            txtContent.ForeColor = Color.White;
            txtContent.BorderStyle = BorderStyle.FixedSingle;
            txtContent.Font = new Font("Microsoft Sans Serif", 10f);

            split.Panel1.Controls.Add(lstSections);
            split.Panel2.Controls.Add(txtContent);
            Controls.Add(split);
            Shown += (s, e) => KeepSectionListCompact();
        }

        private void KeepSectionListCompact()
        {
            if (split == null || split.Width <= 0) return;

            int splitterWidth = Math.Max(1, split.SplitterWidth);
            int panel1Min = Math.Min(120, Math.Max(0, split.Width - splitterWidth));
            int panel2Min = Math.Min(320, Math.Max(0, split.Width - panel1Min - splitterWidth));
            int maxDistance = split.Width - panel2Min - splitterWidth;
            if (maxDistance < panel1Min) return;

            int desired = Math.Max(130, Math.Min(170, split.Width / 5));
            int distance = Math.Max(panel1Min, Math.Min(desired, maxDistance));

            try
            {
                split.Panel1MinSize = 0;
                split.Panel2MinSize = 0;
                split.SplitterDistance = distance;
                split.Panel1MinSize = panel1Min;
                split.Panel2MinSize = panel2Min;
            }
            catch (InvalidOperationException)
            {
                // SplitContainer иногда получает промежуточную ширину во время layout;
                // следующий Resize/Shown повторит расчет уже с валидными границами.
            }
        }

        private void ShowSelectedSection()
        {
            string key = lstSections.SelectedItem as string;
            if (string.IsNullOrWhiteSpace(key)) return;

            string content;
            txtContent.Text = CurrentSections().TryGetValue(key, out content) ? content : "";
        }

        private Dictionary<string, string> CurrentSections()
        {
            return isEnglish ? englishSections : russianSections;
        }

        private void BuildSections()
        {
            russianSections["Общее"] =
                "ACKS Judge's Toolkit объединяет генератор спроса, торговые пути, персонажей и гексовую карту региона.\r\n\r\n" +
                "Кнопка RU/EN переключает язык интерфейса. Данные карт, поселений и персонажей сохраняются локально; интернет для работы программы не нужен.";

            russianSections["Генератор спроса"] =
                "Во вкладке Генератор выберите класс рынка, возраст, климат, воду, высотность, тип поселения и land value.\r\n\r\n" +
                "Тип поселения выбирается одним списком. По умолчанию стоит человек. Человек, клановые люди, орк и зверолюд не меняют demand, но сохраняются как тип для карты; дварф и эльф используют расовые demand-модификаторы.\r\n\r\n" +
                "Город можно сохранить в библиотеку городов, а затем использовать на карте или во вкладке Торговые пути. Водные источники могут складываться: море/океан, озеро и река учитываются отдельно.";

            russianSections["Торговые пути"] =
                "Во вкладке Торговые пути можно вручную сравнить два рынка или выбрать города из библиотеки.\r\n\r\n" +
                "Для влияния спроса нужны две вещи: связь дорогой/водным путем и расстояние в пределах Range of Trade для обоих рынков. При равных рынках оба сдвигаются на 1 пункт навстречу друг другу; при разных меньший рынок сдвигается на 2 пункта к большему.";

            russianSections["Карта"] =
                "Карта использует гексы по 6 миль. Правой кнопкой мыши можно перетаскивать карту, колесом мыши менять масштаб относительно курсора.\r\n\r\n" +
                "Инструменты ставят поселения, крепости, особенности гексов, дороги, реки, местность, высотность и воду. Дороги и реки ставятся двумя кликами по соседним гексам. Поселение можно создать сразу на выбранном гексе с опцией генерации demand по местности, выбором ценности земли 3-9 gp и отдельной опцией влияния соседних поселений.\r\n\r\n" +
                "Переключатели видимости позволяют отдельно скрывать значки местности, дороги, реки, поселения, крепости, домены, особенности гексов, подписи поселений и крепостей, а также включать координаты гексов в нижней части клетки. Координаты по умолчанию выключены. При наведении подпись выбранного места рисуется поверх значков.\r\n\r\n" +
                "В библиотеках карты есть поиск и фильтры: домены можно отбирать по освоенности и типу, державы - по титулу, независимости правителя и расам доменов внутри ветки державы, поселения - по классу рынка и расе.";

            russianSections["Особенности и данжи"] =
                "Особенности гекса - отдельный слой свойств конкретной клетки. На карте они показываются маленьким квадратным значком в левом верхнем углу гекса. Инструмент Особ. позволяет вручную поставить природную особенность, пустой маркер данжа, сгенерированный данж с выбранными типом, размером и уровнем опасности или привязать данж из библиотеки. Двойной клик по значку открывает карточку особенности; для данжа из карточки можно перейти во вкладку Данжи или привязать данж из библиотеки.\r\n\r\n" +
                "Данжи генерируются по уровню опасности, размеру, типу и seed: кнопка генерации каждый раз выставляет новый seed, а тот же seed повторяет результат только при тех же уровне опасности, размере и типе. Уровень опасности ограничен ACKS-диапазоном 1-6 и влияет на встречи, награды и challenge; этажи схемы данжа являются геометрией и могут быть многочисленнее. Размер Логово не создаёт отдельную пустую комнату входа и не генерирует ловушки; если логово состоит из одной комнаты, она становится занятой комнатой с монстром, количеством и сокровищем. Карточка на карте хранит ссылку на DungeonRecord, поэтому сгенерированный на карте данж можно открыть, отредактировать и сохранить обратно в карту. Вкладка Данжи также работает отдельно: можно сгенерировать данж вручную, править комнаты, встречи и сокровища, сохранить его в библиотеку, импортировать/экспортировать XML и экспортировать PNG. PNG-экспорт данжа выводит каждый этаж с номером, легенду дверей/тайных проходов и таблицы блуждающих встреч.\r\n\r\n" +
                "Центральная схема данжа редактируется инструментами: Выбор двигает комнаты, двери и ручные точки изгиба коридоров; клик по коридору выбирает его, а перетаскивание участка добавляет точку изгиба. Связать протягивает коридор/проход/лестницу между двумя комнатами; клик по пустому месту начинает свободный коридор, следующий клик завершает его отрезок. Тип Над/под нужен для прохода другим этажом и рисуется пунктиром, не создавая перекресток с нижними коридорами. Дверь ставит отдельную дверь, тайную дверь или тайный проход только на коридор или стену комнаты, Разрыв удаляет выбранную дверь/тайный проход или проход, Комната добавляет новую комнату в свободное место. У комнат можно менять форму и размер: значения справа или ручки на выбранной комнате меняют габариты прямо на схеме; двери, стоящие на стенах или коридорах, остаются привязанными и двигаются вместе с комнатой. У проходов выбирается тип и ширина, а широкие проходы дают более широкую дверь. Автогенерация строит простые прямоугольные коридоры без декоративных крючков у стен, обходит чужие комнаты и старается не накладывать коридоры друг на друга; комнаты нельзя положить поверх обычного коридора, а настоящие пересечения коридоров показываются узлом. Круглые, овальные и пещерные комнаты соединяются по видимой границе формы, а не по невидимой прямоугольной рамке. Двери ставятся на границах комнат или на сегментах коридоров и поворачиваются вместе с ближайшим сегментом. Колесо мыши масштабирует схему, правая или средняя кнопка двигает viewport, Delete удаляет выбранную дверь, точку изгиба, проход или комнату вместе с её связями. Нижний список показывает ACKS-таблицы блуждающих встреч: для каждого этажа указаны бросок d12, выпавший уровень монстра и строка монстра с количеством.\r\n\r\n" +
                "При полной генерации и перегенерации слоев есть отдельные флажки Особенности гексов и Данжи. Данжи зависят от слоя особенностей: если особенности выключены, данжи не генерируются. Инструмент стирания карты умеет удалять особенности вместе с привязанными данжами, если они больше нигде не используются.\r\n\r\n" +
                "Правила размещения учитывают логику местности: данжи чаще появляются дальше от поселений, но с малым шансом могут быть и внутри поселения. Канализация возможна только в крупных поселениях I-IV класса, затерянный город и поселение в кронах не ставятся в городах, cliff city требует холмов или гор, а treetop settlement требует леса. Природные особенности не избегают доменов и поселений: источник, древние камни, провал или кратер могут оказаться внутри освоенной территории, если подходит местность. Они также проверяют условия: зыбучие пески требуют пустыни, водопад требует реку и перепад высот, вулканы и горячие источники требуют сейсмоактивного региона.\r\n\r\n" +
                "Сейсмичность региона задается в генерации карты. Обычный режим работает как раньше, несейсмоактивный уменьшает долю холмов и гор, а сейсмоактивный увеличивает их и открывает вулканы, гейзеры и горячие источники.";

            russianSections["Сокровища"] =
                "Вкладка Сокровища генерирует наборы по Treasure Type Tables из ACKS Judge's Journal. Можно выбрать Classic или Heroic таблицу, конкретный Treasure Type A-R или подобрать ближайший тип по XP монстров x4.\r\n\r\n" +
                "Для данжей используется тот же процесс: базово берется Classic-таблица. В комнатах с монстрами количество теперь бросается сразу и показывается вместе с формулой кубов, а XP и Treasure Type берутся из каталога монстров по строкам dungeon encounter tables. Пустые комнаты и ловушки используют Unprotected Treasure из STEP 9 с шансами 15% и 30%.\r\n\r\n" +
                "Отдельные кнопки позволяют бросать Treasure Sub-Types: Gems, Jewelry и Special Treasures, а также магические предметы. Для магии Classic поддерживает типы предметов, а Heroic - редкости Common, Uncommon, Rare, Very Rare и Legendary.";

            russianSections["Регионы"] =
                "Кнопка Сгенерировать карту создает карту до 150x150 гексов по seed, размеру, климату, сейсмичности, освоенности, профилю размеров государств и настройкам воды.\r\n\r\n" +
                "Крупная вода строится шумными береговыми линиями с бухтами, мысами и неровными контурами. Побережье не повторяет один и тот же зубчатый паттерн, Континент может получить малые острова, Архипелаг строит неровные острова и на малых картах ужимает их у края вместо ровного среза защитной рамкой, а Два континента создаёт две большие суши внутри рамки карты, омываемые водой, с проливом или морем между ними без прямого среза по краю карты.\r\n\r\n" +
                "Кнопка Перегенерировать слои открывает упрощенную перегенерацию без размера карты, климата и распределения воды. Можно отдельно пересоздать реки, названия объектов, поселения, крепости, домены, дороги, державы, правителей, особенности гексов и данжи; базовая местность, высоты и вода гексов не меняются. При выключенных параметрах слоев перегенерация берет те же пресеты освоенности, что и полная генерация, поэтому Пограничье и Дикие земли сохраняют близкую плотность поселений. Последние настройки окна запоминаются, а seed можно менять кнопкой Новый.\r\n\r\n" +
                "Во вкладке Особые домены перегенерации доступны дварфийские, эльфийские, клановые и переходные домены, включение собственных списков имён и отдельные профили размеров держав для людей, дварфов, эльфов, клановых людей, орков, зверолюдов и переходных доменов. Числовые веса особых доменов включаются отдельным флажком Использовать веса особых доменов и не зависят от флажка параметров слоёв.\r\n\r\n" +
                "В расширенном режиме доступны плотность поселений, генерация крепостей, покрытие доменами, дороги, державы, правители, частота рек/озер, высоты, особые домены и культурные списки имен. Если выбраны дварфийские, эльфийские или клановые домены, генератор заранее резервирует для них подходящие зоны: горы/холмы, леса или глушь вдали от обычной цивилизации. Возраст поселений по умолчанию бросается случайно по классу рынка: крупные рынки чаще старые, но могут быть молодыми. Если поселения и крепости отключены, дороги/домены/державы/правители недоступны. Если домены отключены, державы, вассалы и особые доменные типы недоступны.\r\n\r\n" +
                "Автодороги ограничены дальностью связи: в Диких землях и Пограничье генератор лучше оставит отдельные дорожные острова, чем протянет магистраль через десятки пустых гексов. В Диких землях обычная цивилизация собирается в маленькие очаги по несколько доменов и редкие одиночные владения, а кланхолды ищут удаленные места от любых поселений. Крупные рынки и дварфийские vault могут связываться дальше, но в Wild дварфы, эльфы и кланхолды почти не прокладывают внешние дороги к чужим доменам. Если новая трасса долго идет рядом с уже построенной дорогой, генератор считает ее параллельным дублем и ищет другую связь; хвосты, заканчивающиеся в пустом гексе, удаляются.\r\n\r\n" +
                "Профили держав: Больше малых государств создает множество независимых малых владений, Смешанные размеры оставляет шанс независимым баронам, Больше крупных государств группирует домены в крупные державы, Одно государство объединяет совместимые домены в одну иерархию. В расширенных настройках можно задать отдельные профили для людей, дварфов, эльфов, клановых людей, орков, зверолюдов и переходных доменов; клановые домены по умолчанию независимы.\r\n\r\n" +
                "Державы - политический слой поверх доменов. Генератор строит титульную пирамиду от баронских владений через промежуточные титулы к крупным державам. Культура державы влияет на титулы правителей по таблицам ACKS: Common, Auran, Argollean, Somirean и Jutlandic. В редакторе державы можно вручную задать мужской/общий и женский титул, если нужна своя локальная номенклатура.\r\n\r\n" +
                "В отдельной схеме можно смотреть режим правителей или доменов, приближать и двигать схему, выбирать корневой реалм в списке справа, подниматься к сеньору кнопкой Выше к сеньору и при необходимости кнопкой Показать всё вывести все ветви. Редактирование делается инструментами: Обзор, Связь для протягивания новой линии от нижнего выхода сеньора к верхнему входу вассала и Разрыв для удаления выбранной линии; у одного вассала может быть только один сеньор.";

            russianSections["Домены и крепости"] =
                "Домен может иметь городское поселение, но не обязан. Крепость хранится отдельно: она может быть в поселении или в другом гексе домена.\r\n\r\n" +
                "Отдельная крепость может считаться снабженческим рынком VI класса, но не добавляет городские семьи и не дает городской доход. Дороги учитывают как поселения, так и отдельные крепости. Дварфийские vault, эльфийские fastness и клановые домены имеют отдельные ограничения и иконки.\r\n\r\n" +
                "Клановые поселения людей, орков и зверолюдов ограничены Class VI. Их можно создавать без доменов, если нужна дикая область с редкими особыми поселениями или крепостями. На больших картах Пограничья и Диких земель включенные клановые домены досеиваются россыпью удаленных кланхолдов, а не одним случайным доменом или плотным кластером. У клановых людей есть отдельный варварский набор имён, чтобы они не наследовали названия обычной культуры региона.\r\n\r\n" +
                "По ACKS vault основывается в дварфийском realm или дикой незанятой земле, а fastness - в эльфийском realm или дикой незанятой земле. Поэтому автогенерация не вкладывает такие домены в человеческие владения. При включенных особых типах генератор сначала ставит их опорные поселения и формирует вокруг них горный, лесной или удаленный фронтирный контекст, а затем размещает обычные человеческие поселения. Эльфийские домены требуют подходящего леса и избегают близких человеческих/дварфийских соседей, но их шанс усилен, чтобы выбранный пользователем слой не исчезал полностью.";

            russianSections["Персонажи"] =
                "Во вкладке Персонажи можно создавать, сохранять, импортировать и экспортировать персонажей.\r\n\r\n" +
                "Для случайного NPC уровня выше 0 выставьте уровень выше 0 перед генерацией. Генератор учтет класс, HP по уровню, навыки, внешность, снаряжение и магические вещи. Имена можно генерировать по выбранной культуре.\r\n\r\n" +
                "Библиотека персонажей поддерживает поиск и фильтры по классу, уровню, типу PC/NPC, полу и мировоззрению.";

            russianSections["Генератор названий"] =
                "Вкладка Названия генерирует имена персонажей, фамилии/династии, поселения, домены, державы, реки, озера, моря и океаны.\r\n\r\n" +
                "Выберите культуру, тип названия и количество. Для держав дополнительно выбирается уровень, а для персонажей - пол. Доступны ACKS-культуры титулов Auran, Argollean, Somirean и Jutlandic, а также отдельная культура human_clan для варварских клановых людей. В русском интерфейсе природные названия выводятся в форме Река <название>, Море <название> и Океан <название>.";

            russianSections["Импорт и экспорт"] =
                "Поселения, карты, персонажи и данжи можно экспортировать и импортировать. Карта экспортируется с гексами, дорогами, реками, поселениями, доменами, державами, вассальными связями, особенностями гексов и привязанными данжами.\r\n\r\n" +
                "PNG-экспорт сохраняет всю карту с текущим выбранным слоем и переключателями видимости. Инструмент стирания может очищать поселения, крепости, дороги, реки, домены, местность/воду, особенности гексов и названия по выбранным флажкам. Пользовательские титулы держав входят в Excel-экспорт карты. Если файл создан старой версией программы, недостающие новые поля восстанавливаются при загрузке безопасными значениями.";

            englishSections["Overview"] =
                "ACKS Judge's Toolkit combines demand generation, trade routes, characters, and a 6-mile hex region map.\r\n\r\n" +
                "The RU/EN button switches the interface language. Maps, settlements, and characters are stored locally; the program does not require internet access.";

            englishSections["Demand Generator"] =
                "Use the Generator tab to choose market class, age, biome, water, elevation, settlement type, and land value.\r\n\r\n" +
                "Settlement type is a single-choice list. Human is the default. Human, human clanhold, orc, and beastman do not change demands, but are saved as map placement metadata; dwarf and elf use racial demand modifiers.\r\n\r\n" +
                "A city can be saved to the city library and then used on the map or in Trade Routes. Multiple water sources can apply at once: sea/ocean, lake, and river are checked separately.";

            englishSections["Trade Routes"] =
                "The Trade Routes tab compares two markets manually or from the city library.\r\n\r\n" +
                "A trade route requires a road/water connection and mutual Range of Trade. Equal markets each shift by 1 point toward the other; unequal markets shift the smaller market by 2 points toward the larger.";

            englishSections["Map"] =
                "The map uses 6-mile hexes. Drag with the right mouse button to pan; use the mouse wheel to zoom around the cursor.\r\n\r\n" +
                "Tools place settlements, strongholds, hex features, roads, rivers, terrain, elevation, and water. Roads and rivers are drawn with two clicks on neighboring hexes. A settlement can be generated directly on a hex with options to generate demands from terrain, choose 3-9 gp land value, and apply nearby settlement influence.\r\n\r\n" +
                "Visibility toggles can hide terrain icons, roads, rivers, settlements, strongholds, domains, hex features, settlement labels, and stronghold labels separately, and can enable hex coordinates in the lower part of each cell. Coordinates are off by default. Hovered place labels are drawn above icons.\r\n\r\n" +
                "Map libraries have search and filters: domains by civilization status and type, realms by title, ruler independence, and the races inside the realm branch, and settlements by market class and race.";

            englishSections["Hex Features and Dungeons"] =
                "Hex features are a separate per-cell layer. On the map they appear as a small square icon in the upper-left corner of the hex. The Feature tool can manually place a natural feature, an empty dungeon marker, a generated dungeon with chosen type, size, and level, or a dungeon linked from the library. Double-click the icon to open the feature card; dungeon cards can open the Dungeon tab or link a dungeon from the library.\r\n\r\n" +
                "Dungeons are generated by level, size, type, and seed: Generate rolls a new seed each time, and the same seed repeats a result only with the same level, size, and type. A map feature stores a link to a DungeonRecord, so a map-generated dungeon can be opened, edited, and saved back to the map. The Dungeon tab also works standalone: generate a dungeon manually, edit rooms, encounters, and treasure, save it to the library, import/export XML, and export a PNG. Dungeon PNG export includes every level number, a door/secret-passage legend, and wandering encounter tables.\r\n\r\n" +
                "The central dungeon plan has editing tools: Select moves rooms, doors, and manual corridor bend points; clicking a corridor selects it, and dragging a segment inserts a bend point. Connect draws a corridor/passage/stairs between two rooms; clicking empty space starts a free corridor and the next empty-space click completes its segment. Overpass/underpass marks a passage on another vertical layer and draws as a dashed route without making a junction with lower corridors. Door places a separate door, secret door, or secret passage only on a corridor or room wall, Break removes a selected door/secret passage or passage, and Room adds a new room into free space. Rooms have editable shape and size: the right-side values or the handles on the selected room resize it directly on the plan; doors on room walls or corridors stay anchored and move with the room. Passages have editable type and width, and wider passages draw wider doors. Automatic generation uses simple right-angle corridors without decorative hooks at room walls, routes around unrelated rooms, and tries not to stack corridors on top of each other; rooms cannot be placed over normal corridors, and true corridor crossings are shown as junction nodes. Circular, oval, and cavern rooms connect to the visible shape boundary instead of the invisible bounding rectangle. Generated doors sit on room boundaries or corridor segments and rotate with the nearest segment. The mouse wheel zooms the plan, right or middle drag pans the viewport, Delete removes the selected door, bend point, passage, or room with its links. Danger level follows ACKS 1-6 and affects encounters/rewards; dungeon floors are geometric map levels and can be more numerous. The bottom list shows ACKS wandering encounter tables: for each floor it lists the d12 roll, resulting monster level, and monster row with quantity.\r\n\r\n" +
                "Full generation and layer regeneration have separate Hex features and Dungeons toggles. Dungeons depend on hex features: if features are off, dungeons are not generated. The map erase tool can remove hex features and their linked dungeon records when no other feature references them.\r\n\r\n" +
                "Placement rules use terrain logic: dungeons are more likely farther from settlements but can rarely appear inside one. Sewers require class I-IV settlements, lost cities and treetop settlements cannot be in settlements, cliff cities require hills or mountains, and treetop settlements require forest. Natural features do not avoid domains and settlements: springs, ancient stones, sinkholes, and craters can appear inside settled territory when the terrain fits. They also check conditions: quicksand needs desert, waterfalls need a river and elevation drop, and volcanoes/hot springs require a seismic region.\r\n\r\n" +
                "Region seismicity is selected during map generation. Normal behaves like before, non-seismic reduces hills and mountains, and seismic increases them while enabling volcanoes, geysers, and hot springs.";

            englishSections["Treasures"] =
                "The Treasures tab generates hoards from the ACKS Judge's Journal Treasure Type Tables. You can choose Classic or Heroic tables, pick Treasure Type A-R directly, or select the closest type from monster XP x4.\r\n\r\n" +
                "Dungeon treasure uses the same process and defaults to the Classic table. Monster rooms now roll exact quantity, show the dice expression, and store XP and Treasure Type from the monster catalog used by the dungeon encounter tables. Empty and trap rooms use the STEP 9 Unprotected Treasure table with 15% and 30% chances.\r\n\r\n" +
                "Separate buttons roll Treasure Sub-Types: Gems, Jewelry, and Special Treasures, plus magic items. Classic magic uses item types, while Heroic magic uses Common, Uncommon, Rare, Very Rare, and Legendary rarities.";

            englishSections["Regions"] =
                "Generate Map creates a map up to 150x150 hexes from seed, size, climate, seismicity, civilization level, realm size profile, and water layout.\r\n\r\n" +
                "Large-water layouts use noisy coastlines with bays, capes, and uneven outlines. Sea coast avoids repeating one stamped shoreline pattern, Continent can create small offshore islands, and Two continents creates two large water-surrounded landmasses with a strait or sea between them.\r\n\r\n" +
                "Regenerate Layers opens a simplified layer-regeneration dialog without map size, climate, or water-layout controls. Rivers, feature names, settlements, strongholds, domains, roads, realms, rulers, hex features, and dungeons can be rebuilt separately; base terrain, elevation, and hex water stay unchanged. When layer parameters are off, regeneration uses the same civilization presets as full map generation, so Borderlands and Wild frontier maps keep comparable settlement density. The dialog remembers the last settings, while the New button lets you reroll only the seed.\r\n\r\n" +
                "The Special domains tab exposes dwarven, elven, clanhold, and transitional domains, culture-specific naming toggles, and separate realm-size profiles for humans, dwarves, elves, human clanholds, orcs, beastmen, and transitional domains. Numeric special-domain weights use their own Use special weights checkbox and do not depend on Use layer parameters.\r\n\r\n" +
                "Advanced mode exposes settlement density, stronghold generation, domain coverage, roads, realms, rulers, rivers/lakes, elevation, special domains, and culture-specific naming. When dwarven, elven, or clan domains are enabled, the generator reserves suitable zones for them first: mountains/hills, forests, or remote wilderness away from ordinary civilization. Settlement age defaults to a random market-class profile: larger markets tend older but can still be young. If both settlements and strongholds are off, roads/domains/realms/rulers are unavailable. If domains are off, realms, vassals, and special domain types are unavailable.\r\n\r\n" +
                "Automatic roads have link-distance limits: in Wild and Borderlands maps the generator prefers separate road islands to highways across dozens of empty hexes. In Wild maps, ordinary civilization forms small clusters of a few domains plus rare isolated holdings, while clanholds seek positions away from any settlement. Large markets and dwarven vaults can connect farther, but in Wild maps dwarves, elves, and clanholds almost never build external roads to alien domains. A new route that runs beside an existing road for a long stretch is treated as a parallel duplicate, so the generator looks for another link; stubs ending in empty hexes are removed.\r\n\r\n" +
                "Realm profiles: Mostly small realms creates many independent small holdings, Mixed sizes keeps independent barons possible, Mostly large realms groups domains into larger states, and One realm unites compatible domains into one hierarchy. Advanced settings can override the profile separately for humans, dwarves, elves, human clanholds, orcs, beastmen, and transitional domains; clanholds are independent by default.\r\n\r\n" +
                "Realms are a political layer above domains. The generator builds a title pyramid from baron-level holdings through intermediate titles to large realms. Realm culture controls ruler titles using ACKS-style Common, Auran, Argollean, Somirean, and Jutlandic tables. The realm editor can override the general/male and female title for local naming.\r\n\r\n" +
                "The separate hierarchy scheme has ruler and domain modes, zoom/pan, a right-side root realm list, a Go up button for lieges, and a Show All button. Editing uses explicit tools: View, Link to drag a new line from a liege output to a vassal input, and Break to remove a selected line; one vassal can have only one liege.";

            englishSections["Domains and Strongholds"] =
                "A domain may have an urban settlement, but it does not have to. Strongholds are stored separately and may be inside a settlement or elsewhere in the domain.\r\n\r\n" +
                "A separate stronghold may act as a Class VI supply market, but it does not add urban families or urban income. Road generation uses both settlements and separate strongholds. Dwarven vaults, elven fastnesses, and clanholds have their own restrictions and icons.\r\n\r\n" +
                "Human, orc, and beastman clanhold settlements are limited to Class VI. They can be created without domains when you want a wilderness with sparse special settlements or strongholds. On large Borderlands and Wild maps, enabled clan domains seed scattered remote clanholds instead of collapsing to one random domain or a tight cluster. Human clanholds use a dedicated barbarian name pack instead of inheriting the region's ordinary culture.\r\n\r\n" +
                "By ACKS rules, a vault is founded in a dwarven realm or unclaimed wilderness, and a fastness in an elven realm or unclaimed wilderness. The generator therefore does not place these domains inside human holdings. When special domain types are enabled, the generator first places their anchor settlements and shapes a mountain, forest, or remote frontier context around them, then places ordinary human settlements. Elven domains require suitable forest and avoid close human/dwarven neighbors, but their chance is boosted so an enabled elven layer does not vanish entirely.";

            englishSections["Characters"] =
                "The Characters tab creates, saves, imports, and exports characters.\r\n\r\n" +
                "For an NPC above 0 level, set level above 0 before random generation. The generator handles class, leveled HP, proficiencies, appearance, equipment, and magic items. Names can be generated by culture.\r\n\r\n" +
                "The character library supports search and filters by class, level, PC/NPC type, sex, and alignment.";

            englishSections["Name Generator"] =
                "The Names tab generates personal names, surnames/dynasties, settlements, domains, realms, rivers, lakes, seas, and oceans.\r\n\r\n" +
                "Choose culture, name type, and count. Realm names also use a tier, while personal names can use gender. ACKS title cultures Auran, Argollean, Somirean, and Jutlandic are available, along with a human_clan culture for barbarian clanholds. In Russian mode, natural features use forms such as Река <name>, Море <name>, and Океан <name>.";

            englishSections["Import and Export"] =
                "Settlements, maps, characters, and dungeons can be imported and exported. Map export includes hexes, roads, rivers, settlements, domains, realms, vassal links, hex features, and linked dungeon records.\r\n\r\n" +
                "PNG export saves the full map using the currently selected layer and visibility toggles. The erase tool can clear settlements, strongholds, roads, rivers, domains, terrain/water, hex features, and names according to its checkboxes. Realm custom titles are included in map Excel export. If a file was made by an older version, missing fields are restored with safe defaults on load.";
        }
    }
}
