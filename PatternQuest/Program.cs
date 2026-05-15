using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace PatternQuest
{
    public static class Program
    {
        [STAThread]
        public static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new GameForm());
        }
    }

    public sealed class GameForm : Form, IGameObserver
    {
        private readonly ListBox _playerList = new ListBox();
        private readonly ListBox _enemyList = new ListBox();
        private readonly TextBox _journal = new TextBox();
        private readonly NumericUpDown _attackerIndex = new NumericUpDown();
        private readonly NumericUpDown _enemyIndex = new NumericUpDown();
        private readonly NumericUpDown _healIndex = new NumericUpDown();
        private readonly Button _scoutButton = new Button();
        private readonly Button _attackButton = new Button();
        private readonly Button _healButton = new Button();
        private readonly Button _endButton = new Button();
        private readonly Label _mapLabel = new Label();

        private readonly CommandParser _parser = new CommandParser();
        private readonly GameContext _context;

        public GameForm()
        {
            Text = "Pattern Quest - лабораторные 7-8";
            ClientSize = new Size(820, 520);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            StartPosition = FormStartPosition.CenterScreen;
            Font = new Font("Segoe UI", 9F);

            EventBus events = new EventBus();
            events.Attach(this);
            events.Attach(new ScoreBoard());
            Logger.Instance.MessageWritten += AppendLog;

            IFactionFactory playerFactory = new OrderFactory();
            IFactionFactory enemyFactory = new RaidersFactory();
            ArmyDirector director = new ArmyDirector();
            Squad player = director.CreateBalancedArmy(playerFactory);
            Squad enemy = director.CreateBalancedArmy(enemyFactory);
            IMapImage map = playerFactory.CreateMap();

            _context = new GameContext(player, enemy, map, events);
            BuildUi();
            Logger.Instance.Write("Игра запущена. Использованы Singleton, Abstract Factory, Builder, Composite, Proxy, Interpreter, Observer.");
            RefreshArmies();
        }

        public void Update(GameEvent gameEvent)
        {
            AppendLog(gameEvent.Message);
            RefreshArmies();

            if (_context.BattleEnded)
            {
                _attackButton.Enabled = false;
                _healButton.Enabled = false;
                _scoutButton.Enabled = false;
                _endButton.Enabled = false;
            }
        }

        private void BuildUi()
        {
            Label title = new Label();
            title.Text = "Pattern Quest";
            title.Font = new Font(Font.FontFamily, 16F, FontStyle.Bold);
            title.Location = new Point(16, 12);
            title.Size = new Size(260, 32);
            Controls.Add(title);

            _mapLabel.Text = "Карта: область скрыта";
            _mapLabel.BorderStyle = BorderStyle.FixedSingle;
            _mapLabel.BackColor = Color.FromArgb(235, 239, 244);
            _mapLabel.Location = new Point(300, 14);
            _mapLabel.Size = new Size(500, 30);
            _mapLabel.TextAlign = ContentAlignment.MiddleCenter;
            Controls.Add(_mapLabel);

            Label playerTitle = new Label();
            playerTitle.Text = "Орден Севера";
            playerTitle.Location = new Point(16, 62);
            playerTitle.Size = new Size(360, 22);
            Controls.Add(playerTitle);

            Label enemyTitle = new Label();
            enemyTitle.Text = "Налетчики Пустоши";
            enemyTitle.Location = new Point(416, 62);
            enemyTitle.Size = new Size(360, 22);
            Controls.Add(enemyTitle);

            _playerList.Location = new Point(16, 86);
            _playerList.Size = new Size(380, 160);
            Controls.Add(_playerList);

            _enemyList.Location = new Point(416, 86);
            _enemyList.Size = new Size(380, 160);
            Controls.Add(_enemyList);

            _scoutButton.Text = "Разведка";
            _scoutButton.Location = new Point(16, 266);
            _scoutButton.Size = new Size(110, 32);
            _scoutButton.Click += delegate { Execute("scout"); };
            Controls.Add(_scoutButton);

            Label attackLabel = new Label();
            attackLabel.Text = "Атака: свой";
            attackLabel.Location = new Point(150, 272);
            attackLabel.Size = new Size(78, 24);
            Controls.Add(attackLabel);

            _attackerIndex.Location = new Point(230, 270);
            _attackerIndex.Size = new Size(48, 24);
            _attackerIndex.Minimum = 0;
            _attackerIndex.Maximum = 4;
            Controls.Add(_attackerIndex);

            Label targetLabel = new Label();
            targetLabel.Text = "враг";
            targetLabel.Location = new Point(288, 272);
            targetLabel.Size = new Size(40, 24);
            Controls.Add(targetLabel);

            _enemyIndex.Location = new Point(330, 270);
            _enemyIndex.Size = new Size(48, 24);
            _enemyIndex.Minimum = 0;
            _enemyIndex.Maximum = 4;
            Controls.Add(_enemyIndex);

            _attackButton.Text = "Атаковать";
            _attackButton.Location = new Point(390, 266);
            _attackButton.Size = new Size(110, 32);
            _attackButton.Click += delegate { Execute("attack " + _attackerIndex.Value + " " + _enemyIndex.Value); };
            Controls.Add(_attackButton);

            Label healLabel = new Label();
            healLabel.Text = "Лечение: свой";
            healLabel.Location = new Point(524, 272);
            healLabel.Size = new Size(92, 24);
            Controls.Add(healLabel);

            _healIndex.Location = new Point(620, 270);
            _healIndex.Size = new Size(48, 24);
            _healIndex.Minimum = 0;
            _healIndex.Maximum = 4;
            Controls.Add(_healIndex);

            _healButton.Text = "Лечить";
            _healButton.Location = new Point(686, 266);
            _healButton.Size = new Size(110, 32);
            _healButton.Click += delegate { Execute("heal " + _healIndex.Value); };
            Controls.Add(_healButton);

            _journal.Location = new Point(16, 320);
            _journal.Size = new Size(780, 150);
            _journal.Multiline = true;
            _journal.ScrollBars = ScrollBars.Vertical;
            _journal.ReadOnly = true;
            Controls.Add(_journal);

            _endButton.Text = "Завершить бой";
            _endButton.Location = new Point(650, 480);
            _endButton.Size = new Size(146, 30);
            _endButton.Click += delegate { Execute("end"); };
            Controls.Add(_endButton);
        }

        private void Execute(string command)
        {
            AppendLog("> " + command);
            _parser.Parse(command).Interpret(_context);
        }

        private void RefreshArmies()
        {
            FillArmyList(_playerList, _context.PlayerArmy);
            FillArmyList(_enemyList, _context.EnemyArmy);
            _mapLabel.Text = _context.MapDescription;
        }

        private static void FillArmyList(ListBox listBox, Squad army)
        {
            listBox.Items.Clear();
            List<Unit> units = army.Units();
            listBox.Items.Add(army.Name + " | сила: " + army.Power);
            for (int i = 0; i < units.Count; i++)
            {
                Unit unit = units[i];
                string status = unit.IsAlive ? "жив" : "выбыл";
                listBox.Items.Add(i + ". " + unit.Name + " | HP " + unit.Health + " | ATK " + unit.Attack + " | " + status);
            }
        }

        private void AppendLog(string message)
        {
            _journal.AppendText(message + Environment.NewLine);
        }
    }

    // Singleton: единый журнал игры.
    public sealed class Logger
    {
        private static readonly Logger _instance = new Logger();

        private Logger() { }

        public static Logger Instance
        {
            get { return _instance; }
        }

        public event Action<string> MessageWritten;

        public void Write(string message)
        {
            if (MessageWritten != null)
            {
                MessageWritten("[LOG] " + message);
            }
        }
    }

    // Observer: подписчики получают игровые события.
    public interface IGameObserver
    {
        void Update(GameEvent gameEvent);
    }

    public sealed class GameEvent
    {
        public GameEvent(string type, string message)
        {
            Type = type;
            Message = message;
        }

        public string Type { get; private set; }
        public string Message { get; private set; }
    }

    public sealed class EventBus
    {
        private readonly List<IGameObserver> _observers = new List<IGameObserver>();

        public void Attach(IGameObserver observer)
        {
            _observers.Add(observer);
        }

        public void Notify(GameEvent gameEvent)
        {
            for (int i = 0; i < _observers.Count; i++)
            {
                _observers[i].Update(gameEvent);
            }
        }
    }

    public sealed class ScoreBoard : IGameObserver
    {
        private int _attacks;
        private int _heals;

        public void Update(GameEvent gameEvent)
        {
            if (gameEvent.Type == "attack")
            {
                _attacks++;
            }
            if (gameEvent.Type == "heal")
            {
                _heals++;
            }

            Logger.Instance.Write("Статистика: атак " + _attacks + ", лечений " + _heals);
        }
    }

    // Abstract Factory: фракция создает совместимое семейство объектов.
    public interface IFactionFactory
    {
        string FactionName { get; }
        Unit CreateInfantry();
        Unit CreateArcher();
        Unit CreateSupport();
        IMapImage CreateMap();
    }

    public sealed class OrderFactory : IFactionFactory
    {
        public string FactionName { get { return "Орден Севера"; } }

        public Unit CreateInfantry()
        {
            return new Unit("Страж", 34, 8, 0);
        }

        public Unit CreateArcher()
        {
            return new Unit("Арбалетчик", 24, 11, 0);
        }

        public Unit CreateSupport()
        {
            return new Unit("Целитель", 22, 4, 12);
        }

        public IMapImage CreateMap()
        {
            return new MapProxy("snow-pass.map");
        }
    }

    public sealed class RaidersFactory : IFactionFactory
    {
        public string FactionName { get { return "Налетчики Пустоши"; } }

        public Unit CreateInfantry()
        {
            return new Unit("Берсерк", 30, 10, 0);
        }

        public Unit CreateArcher()
        {
            return new Unit("Метатель", 22, 9, 0);
        }

        public Unit CreateSupport()
        {
            return new Unit("Шаман", 20, 5, 9);
        }

        public IMapImage CreateMap()
        {
            return new MapProxy("dry-canyon.map");
        }
    }

    // Composite: Unit и Squad имеют общий интерфейс.
    public interface IArmyComponent
    {
        string Name { get; }
        int Power { get; }
        bool IsAlive { get; }
    }

    public sealed class Unit : IArmyComponent
    {
        private readonly int _maxHealth;

        public Unit(string name, int health, int attack, int healing)
        {
            Name = name;
            _maxHealth = health;
            Health = health;
            Attack = attack;
            Healing = healing;
        }

        public string Name { get; private set; }
        public int Health { get; private set; }
        public int Attack { get; private set; }
        public int Healing { get; private set; }

        public int Power
        {
            get { return IsAlive ? Attack + Health / 4 + Healing / 2 : 0; }
        }

        public bool IsAlive
        {
            get { return Health > 0; }
        }

        public void TakeDamage(int damage)
        {
            Health -= damage;
            if (Health < 0)
            {
                Health = 0;
            }
        }

        public void Heal(Unit target)
        {
            if (!IsAlive || Healing <= 0)
            {
                return;
            }

            target.Health += Healing;
            if (target.Health > target._maxHealth)
            {
                target.Health = target._maxHealth;
            }
        }
    }

    public sealed class Squad : IArmyComponent
    {
        private readonly List<IArmyComponent> _children = new List<IArmyComponent>();

        public Squad(string name)
        {
            Name = name;
        }

        public string Name { get; private set; }

        public int Power
        {
            get
            {
                int power = 0;
                for (int i = 0; i < _children.Count; i++)
                {
                    power += _children[i].Power;
                }
                return power;
            }
        }

        public bool IsAlive
        {
            get
            {
                for (int i = 0; i < _children.Count; i++)
                {
                    if (_children[i].IsAlive)
                    {
                        return true;
                    }
                }
                return false;
            }
        }

        public void Add(IArmyComponent component)
        {
            _children.Add(component);
        }

        public List<Unit> Units()
        {
            List<Unit> units = new List<Unit>();
            CollectUnits(this, units);
            return units;
        }

        private static void CollectUnits(IArmyComponent component, List<Unit> units)
        {
            Unit unit = component as Unit;
            if (unit != null)
            {
                units.Add(unit);
                return;
            }

            Squad squad = component as Squad;
            if (squad == null)
            {
                return;
            }

            for (int i = 0; i < squad._children.Count; i++)
            {
                CollectUnits(squad._children[i], units);
            }
        }
    }

    // Builder: управляемая сборка армии.
    public sealed class ArmyBuilder
    {
        private readonly IFactionFactory _factory;
        private readonly Squad _army;

        public ArmyBuilder(IFactionFactory factory)
        {
            _factory = factory;
            _army = new Squad(factory.FactionName);
        }

        public ArmyBuilder AddVanguard()
        {
            Squad squad = new Squad("Авангард");
            squad.Add(_factory.CreateInfantry());
            squad.Add(_factory.CreateInfantry());
            _army.Add(squad);
            return this;
        }

        public ArmyBuilder AddRangeLine()
        {
            Squad squad = new Squad("Дальний ряд");
            squad.Add(_factory.CreateArcher());
            squad.Add(_factory.CreateArcher());
            _army.Add(squad);
            return this;
        }

        public ArmyBuilder AddSupport()
        {
            Squad squad = new Squad("Поддержка");
            squad.Add(_factory.CreateSupport());
            _army.Add(squad);
            return this;
        }

        public Squad Build()
        {
            return _army;
        }
    }

    public sealed class ArmyDirector
    {
        public Squad CreateBalancedArmy(IFactionFactory factory)
        {
            return new ArmyBuilder(factory)
                .AddVanguard()
                .AddRangeLine()
                .AddSupport()
                .Build();
        }
    }

    // Proxy: настоящая карта загружается только после разведки.
    public interface IMapImage
    {
        string Description { get; }
        void Reveal();
    }

    public sealed class RealMapImage : IMapImage
    {
        private readonly string _fileName;

        public RealMapImage(string fileName)
        {
            _fileName = fileName;
            Logger.Instance.Write("Загрузка реальной карты " + fileName);
        }

        public string Description
        {
            get { return "Карта: открыта (" + _fileName + ")"; }
        }

        public void Reveal() { }
    }

    public sealed class MapProxy : IMapImage
    {
        private readonly string _fileName;
        private RealMapImage _realMap;

        public MapProxy(string fileName)
        {
            _fileName = fileName;
        }

        public string Description
        {
            get { return _realMap == null ? "Карта: скрытая область (" + _fileName + ")" : _realMap.Description; }
        }

        public void Reveal()
        {
            if (_realMap == null)
            {
                _realMap = new RealMapImage(_fileName);
            }
            _realMap.Reveal();
        }
    }

    // Interpreter: мини-язык команд игрока.
    public interface ICommandExpression
    {
        bool Interpret(GameContext context);
    }

    public sealed class AttackExpression : ICommandExpression
    {
        private readonly int _attackerIndex;
        private readonly int _targetIndex;

        public AttackExpression(int attackerIndex, int targetIndex)
        {
            _attackerIndex = attackerIndex;
            _targetIndex = targetIndex;
        }

        public bool Interpret(GameContext context)
        {
            context.Attack(_attackerIndex, _targetIndex);
            return true;
        }
    }

    public sealed class HealExpression : ICommandExpression
    {
        private readonly int _targetIndex;

        public HealExpression(int targetIndex)
        {
            _targetIndex = targetIndex;
        }

        public bool Interpret(GameContext context)
        {
            context.Heal(_targetIndex);
            return true;
        }
    }

    public sealed class ScoutExpression : ICommandExpression
    {
        public bool Interpret(GameContext context)
        {
            context.Scout();
            return true;
        }
    }

    public sealed class EndExpression : ICommandExpression
    {
        public bool Interpret(GameContext context)
        {
            context.EndBattle();
            return false;
        }
    }

    public sealed class InvalidExpression : ICommandExpression
    {
        private readonly string _command;

        public InvalidExpression(string command)
        {
            _command = command;
        }

        public bool Interpret(GameContext context)
        {
            context.Events.Notify(new GameEvent("error", "Команда не распознана: " + _command));
            return true;
        }
    }

    public sealed class CommandParser
    {
        public ICommandExpression Parse(string line)
        {
            string[] parts = line.Trim().Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            int first;
            int second;

            if (parts.Length == 1 && parts[0] == "scout")
            {
                return new ScoutExpression();
            }
            if (parts.Length == 1 && parts[0] == "end")
            {
                return new EndExpression();
            }
            if (parts.Length == 2 && parts[0] == "heal" && int.TryParse(parts[1], out first))
            {
                return new HealExpression(first);
            }
            if (parts.Length == 3 && parts[0] == "attack" &&
                int.TryParse(parts[1], out first) &&
                int.TryParse(parts[2], out second))
            {
                return new AttackExpression(first, second);
            }

            return new InvalidExpression(line);
        }
    }

    public sealed class GameContext
    {
        private readonly IMapImage _map;

        public GameContext(Squad playerArmy, Squad enemyArmy, IMapImage map, EventBus events)
        {
            PlayerArmy = playerArmy;
            EnemyArmy = enemyArmy;
            _map = map;
            Events = events;
        }

        public Squad PlayerArmy { get; private set; }
        public Squad EnemyArmy { get; private set; }
        public EventBus Events { get; private set; }
        public bool BattleEnded { get; private set; }

        public string MapDescription
        {
            get { return _map.Description; }
        }

        public void Scout()
        {
            _map.Reveal();
            Events.Notify(new GameEvent("scout", "Разведка открыла карту и позиции противника."));
        }

        public void Attack(int attackerIndex, int targetIndex)
        {
            List<Unit> attackers = PlayerArmy.Units();
            List<Unit> targets = EnemyArmy.Units();
            if (!CheckIndex(attackers, attackerIndex, "атакующего") ||
                !CheckIndex(targets, targetIndex, "цели"))
            {
                return;
            }

            Unit attacker = attackers[attackerIndex];
            Unit target = targets[targetIndex];
            if (!attacker.IsAlive)
            {
                Events.Notify(new GameEvent("error", attacker.Name + " не может атаковать, потому что выбыл из боя."));
                return;
            }

            target.TakeDamage(attacker.Attack);
            Events.Notify(new GameEvent("attack", attacker.Name + " атакует " + target.Name + " на " + attacker.Attack + " урона."));
            EnemyTurn();
            CheckVictory();
        }

        public void Heal(int targetIndex)
        {
            List<Unit> units = PlayerArmy.Units();
            if (!CheckIndex(units, targetIndex, "союзника"))
            {
                return;
            }

            Unit healer = null;
            for (int i = 0; i < units.Count; i++)
            {
                if (units[i].Healing > 0 && units[i].IsAlive)
                {
                    healer = units[i];
                    break;
                }
            }

            if (healer == null)
            {
                Events.Notify(new GameEvent("error", "В армии нет живого лекаря."));
                return;
            }

            Unit target = units[targetIndex];
            healer.Heal(target);
            Events.Notify(new GameEvent("heal", healer.Name + " лечит " + target.Name + "."));
            EnemyTurn();
            CheckVictory();
        }

        public void EndBattle()
        {
            BattleEnded = true;
            Events.Notify(new GameEvent("end", "Бой завершен по команде игрока."));
        }

        private bool CheckIndex(List<Unit> units, int index, string role)
        {
            if (index < 0 || index >= units.Count)
            {
                Events.Notify(new GameEvent("error", "Неверный номер " + role + ": " + index));
                return false;
            }
            return true;
        }

        private void EnemyTurn()
        {
            Unit enemy = FirstAlive(EnemyArmy.Units());
            Unit player = FirstAlive(PlayerArmy.Units());
            if (enemy == null || player == null)
            {
                return;
            }

            player.TakeDamage(enemy.Attack);
            Events.Notify(new GameEvent("attack", "Ответный ход: " + enemy.Name + " атакует " + player.Name + "."));
        }

        private static Unit FirstAlive(List<Unit> units)
        {
            for (int i = 0; i < units.Count; i++)
            {
                if (units[i].IsAlive)
                {
                    return units[i];
                }
            }
            return null;
        }

        private void CheckVictory()
        {
            if (!PlayerArmy.IsAlive || !EnemyArmy.IsAlive)
            {
                BattleEnded = true;
                string winner = PlayerArmy.IsAlive ? PlayerArmy.Name : EnemyArmy.Name;
                Events.Notify(new GameEvent("finish", "Победитель: " + winner));
            }
        }
    }
}
