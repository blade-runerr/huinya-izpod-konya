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
            Application.Run(new ShooterForm());
        }
    }

    public sealed class ShooterForm : Form, IGameObserver
    {
        private readonly Timer _timer = new Timer();
        private readonly HashSet<Keys> _keys = new HashSet<Keys>();
        private readonly Label _status = new Label();
        private readonly TextBox _journal = new TextBox();
        private readonly GameWorld _world;
        private readonly CommandInterpreter _interpreter = new CommandInterpreter();

        public ShooterForm()
        {
            Text = "Pattern Quest Shooter - лабораторные 7-8";
            ClientSize = new Size(900, 620);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            StartPosition = FormStartPosition.CenterScreen;
            DoubleBuffered = true;
            KeyPreview = true;
            BackColor = Color.FromArgb(18, 24, 33);

            EventBus events = new EventBus();
            events.Attach(this);
            events.Attach(new ScoreObserver());
            Logger.Instance.MessageWritten += AddLog;

            IFactionFactory factory = new SpaceFactionFactory();
            WaveDirector director = new WaveDirector();
            Fleet fleet = director.CreateFirstWave(factory);
            _world = new GameWorld(factory.CreatePlayer(), fleet, factory.CreateBackground(), events);

            BuildUi();

            _timer.Interval = 25;
            _timer.Tick += delegate
            {
                _interpreter.Interpret(_keys, _world);
                _world.Update();
                RefreshStatus();
                Invalidate();
            };
            _timer.Start();

            KeyDown += OnKeyDown;
            KeyUp += OnKeyUp;
            Logger.Instance.Write("Стрелялка запущена. Управление: A/D или стрелки, Space - выстрел, R - рестарт.");
        }

        public void Update(GameEvent gameEvent)
        {
            AddLog(gameEvent.Message);
            RefreshStatus();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            _world.Draw(e.Graphics);

            using (Brush textBrush = new SolidBrush(Color.White))
            {
                e.Graphics.DrawString("A/D или ←/→ - движение, Space - стрелять, R - рестарт",
                    Font, textBrush, 18, 46);
            }
        }

        private void BuildUi()
        {
            _status.Location = new Point(16, 12);
            _status.Size = new Size(860, 26);
            _status.ForeColor = Color.White;
            _status.Font = new Font("Segoe UI", 11F, FontStyle.Bold);
            Controls.Add(_status);

            _journal.Location = new Point(16, 500);
            _journal.Size = new Size(868, 104);
            _journal.Multiline = true;
            _journal.ReadOnly = true;
            _journal.ScrollBars = ScrollBars.Vertical;
            _journal.BackColor = Color.FromArgb(10, 14, 20);
            _journal.ForeColor = Color.FromArgb(220, 235, 255);
            Controls.Add(_journal);

            RefreshStatus();
        }

        private void OnKeyDown(object sender, KeyEventArgs e)
        {
            _keys.Add(e.KeyCode);
            if (e.KeyCode == Keys.R)
            {
                _world.Restart();
            }
        }

        private void OnKeyUp(object sender, KeyEventArgs e)
        {
            _keys.Remove(e.KeyCode);
        }

        private void RefreshStatus()
        {
            _status.Text = _world.StatusText;
        }

        private void AddLog(string message)
        {
            _journal.AppendText(message + Environment.NewLine);
        }
    }

    // Singleton: единый игровой лог.
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

    // Observer: окно и счетчик получают события мира.
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

    public sealed class ScoreObserver : IGameObserver
    {
        private int _hits;

        public void Update(GameEvent gameEvent)
        {
            if (gameEvent.Type == "hit")
            {
                _hits++;
                Logger.Instance.Write("Попаданий за игру: " + _hits);
            }
            if (gameEvent.Type == "restart")
            {
                _hits = 0;
            }
        }
    }

    // Abstract Factory: создает семейство объектов одной темы.
    public interface IFactionFactory
    {
        PlayerShip CreatePlayer();
        EnemyShip CreateScout(int x, int y);
        EnemyShip CreateRaider(int x, int y);
        IBackground CreateBackground();
    }

    public sealed class SpaceFactionFactory : IFactionFactory
    {
        public PlayerShip CreatePlayer()
        {
            return new PlayerShip(420, 440);
        }

        public EnemyShip CreateScout(int x, int y)
        {
            return new EnemyShip("Разведчик", x, y, 30, 2, 10, Color.FromArgb(255, 195, 85));
        }

        public EnemyShip CreateRaider(int x, int y)
        {
            return new EnemyShip("Рейдер", x, y, 44, 1, 20, Color.FromArgb(255, 95, 110));
        }

        public IBackground CreateBackground()
        {
            return new BackgroundProxy();
        }
    }

    // Builder: собирает волну врагов.
    public sealed class WaveBuilder
    {
        private readonly IFactionFactory _factory;
        private readonly Fleet _fleet = new Fleet("Вражеская волна");

        public WaveBuilder(IFactionFactory factory)
        {
            _factory = factory;
        }

        public WaveBuilder AddScoutLine()
        {
            for (int i = 0; i < 6; i++)
            {
                _fleet.Add(_factory.CreateScout(90 + i * 115, 95));
            }
            return this;
        }

        public WaveBuilder AddRaiderLine()
        {
            for (int i = 0; i < 4; i++)
            {
                _fleet.Add(_factory.CreateRaider(150 + i * 145, 155));
            }
            return this;
        }

        public Fleet Build()
        {
            return _fleet;
        }
    }

    public sealed class WaveDirector
    {
        public Fleet CreateFirstWave(IFactionFactory factory)
        {
            return new WaveBuilder(factory)
                .AddScoutLine()
                .AddRaiderLine()
                .Build();
        }
    }

    // Composite: отдельный враг и флот имеют общий интерфейс.
    public interface IEnemyComponent
    {
        bool IsAlive { get; }
        void Update();
        void Draw(Graphics graphics);
        void Collect(List<EnemyShip> enemies);
    }

    public sealed class Fleet : IEnemyComponent
    {
        private readonly List<IEnemyComponent> _children = new List<IEnemyComponent>();

        public Fleet(string name)
        {
            Name = name;
        }

        public string Name { get; private set; }

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

        public void Add(IEnemyComponent component)
        {
            _children.Add(component);
        }

        public void Update()
        {
            for (int i = 0; i < _children.Count; i++)
            {
                _children[i].Update();
            }
        }

        public void Draw(Graphics graphics)
        {
            for (int i = 0; i < _children.Count; i++)
            {
                _children[i].Draw(graphics);
            }
        }

        public void Collect(List<EnemyShip> enemies)
        {
            for (int i = 0; i < _children.Count; i++)
            {
                _children[i].Collect(enemies);
            }
        }
    }

    public sealed class EnemyShip : IEnemyComponent
    {
        private int _direction = 1;
        private readonly Color _color;

        public EnemyShip(string name, int x, int y, int size, int speed, int score, Color color)
        {
            Name = name;
            Bounds = new Rectangle(x, y, size, size);
            Speed = speed;
            Score = score;
            _color = color;
            IsAlive = true;
        }

        public string Name { get; private set; }
        public Rectangle Bounds { get; private set; }
        public int Speed { get; private set; }
        public int Score { get; private set; }
        public bool IsAlive { get; private set; }

        public void Destroy()
        {
            IsAlive = false;
        }

        public void Update()
        {
            if (!IsAlive)
            {
                return;
            }

            Rectangle next = Bounds;
            next.X += Speed * _direction;
            if (next.Left < 18 || next.Right > 882)
            {
                _direction *= -1;
                next.X += Speed * _direction;
                next.Y += 20;
            }
            Bounds = next;
        }

        public void Draw(Graphics graphics)
        {
            if (!IsAlive)
            {
                return;
            }

            using (Brush brush = new SolidBrush(_color))
            {
                Point[] ship =
                {
                    new Point(Bounds.Left + Bounds.Width / 2, Bounds.Bottom),
                    new Point(Bounds.Left, Bounds.Top),
                    new Point(Bounds.Right, Bounds.Top)
                };
                graphics.FillPolygon(brush, ship);
            }
        }

        public void Collect(List<EnemyShip> enemies)
        {
            if (IsAlive)
            {
                enemies.Add(this);
            }
        }
    }

    // Proxy: фон "загружается" при первом рисовании, а не при старте формы.
    public interface IBackground
    {
        void Draw(Graphics graphics, Rectangle area);
    }

    public sealed class RealBackground : IBackground
    {
        private readonly List<Point> _stars = new List<Point>();

        public RealBackground()
        {
            Random random = new Random(12);
            for (int i = 0; i < 90; i++)
            {
                _stars.Add(new Point(random.Next(20, 880), random.Next(70, 490)));
            }
            Logger.Instance.Write("Фон уровня загружен через Proxy.");
        }

        public void Draw(Graphics graphics, Rectangle area)
        {
            using (Brush background = new SolidBrush(Color.FromArgb(18, 24, 33)))
            using (Brush star = new SolidBrush(Color.FromArgb(180, 220, 255)))
            {
                graphics.FillRectangle(background, area);
                for (int i = 0; i < _stars.Count; i++)
                {
                    graphics.FillEllipse(star, _stars[i].X, _stars[i].Y, 2, 2);
                }
            }
        }
    }

    public sealed class BackgroundProxy : IBackground
    {
        private RealBackground _background;

        public void Draw(Graphics graphics, Rectangle area)
        {
            if (_background == null)
            {
                _background = new RealBackground();
            }
            _background.Draw(graphics, area);
        }
    }

    public sealed class PlayerShip
    {
        public PlayerShip(int x, int y)
        {
            Bounds = new Rectangle(x, y, 44, 32);
            Health = 3;
        }

        public Rectangle Bounds { get; private set; }
        public int Health { get; private set; }
        public int Cooldown { get; private set; }

        public void Move(int dx)
        {
            Rectangle next = Bounds;
            next.X += dx;
            if (next.Left < 18)
            {
                next.X = 18;
            }
            if (next.Right > 882)
            {
                next.X = 882 - next.Width;
            }
            Bounds = next;
        }

        public Bullet TryShoot()
        {
            if (Cooldown > 0)
            {
                return null;
            }

            Cooldown = 10;
            return new Bullet(Bounds.Left + Bounds.Width / 2 - 3, Bounds.Top - 14);
        }

        public void Update()
        {
            if (Cooldown > 0)
            {
                Cooldown--;
            }
        }

        public void Damage()
        {
            Health--;
        }

        public void Draw(Graphics graphics)
        {
            using (Brush body = new SolidBrush(Color.FromArgb(95, 185, 255)))
            using (Brush core = new SolidBrush(Color.White))
            {
                Point[] ship =
                {
                    new Point(Bounds.Left + Bounds.Width / 2, Bounds.Top),
                    new Point(Bounds.Left, Bounds.Bottom),
                    new Point(Bounds.Right, Bounds.Bottom)
                };
                graphics.FillPolygon(body, ship);
                graphics.FillEllipse(core, Bounds.Left + 16, Bounds.Top + 13, 12, 12);
            }
        }
    }

    public sealed class Bullet
    {
        public Bullet(int x, int y)
        {
            Bounds = new Rectangle(x, y, 6, 16);
            IsActive = true;
        }

        public Rectangle Bounds { get; private set; }
        public bool IsActive { get; private set; }

        public void Update()
        {
            Rectangle next = Bounds;
            next.Y -= 12;
            Bounds = next;
            if (Bounds.Bottom < 70)
            {
                IsActive = false;
            }
        }

        public void Deactivate()
        {
            IsActive = false;
        }

        public void Draw(Graphics graphics)
        {
            if (!IsActive)
            {
                return;
            }

            using (Brush brush = new SolidBrush(Color.FromArgb(160, 255, 180)))
            {
                graphics.FillRectangle(brush, Bounds);
            }
        }
    }

    // Interpreter: клавиши переводятся в команды игрового мира.
    public sealed class CommandInterpreter
    {
        public void Interpret(HashSet<Keys> keys, GameWorld world)
        {
            if (keys.Contains(Keys.Left) || keys.Contains(Keys.A))
            {
                new MoveCommand(-7).Execute(world);
            }
            if (keys.Contains(Keys.Right) || keys.Contains(Keys.D))
            {
                new MoveCommand(7).Execute(world);
            }
            if (keys.Contains(Keys.Space))
            {
                new ShootCommand().Execute(world);
            }
        }
    }

    public interface IGameCommand
    {
        void Execute(GameWorld world);
    }

    public sealed class MoveCommand : IGameCommand
    {
        private readonly int _dx;

        public MoveCommand(int dx)
        {
            _dx = dx;
        }

        public void Execute(GameWorld world)
        {
            world.MovePlayer(_dx);
        }
    }

    public sealed class ShootCommand : IGameCommand
    {
        public void Execute(GameWorld world)
        {
            world.PlayerShoot();
        }
    }

    public sealed class GameWorld
    {
        private readonly EventBus _events;
        private readonly IFactionFactory _factory = new SpaceFactionFactory();
        private readonly WaveDirector _director = new WaveDirector();
        private PlayerShip _player;
        private Fleet _fleet;
        private readonly IBackground _background;
        private readonly List<Bullet> _bullets = new List<Bullet>();
        private bool _gameOver;

        public GameWorld(PlayerShip player, Fleet fleet, IBackground background, EventBus events)
        {
            _player = player;
            _fleet = fleet;
            _background = background;
            _events = events;
        }

        public int Score { get; private set; }

        public string StatusText
        {
            get
            {
                if (_gameOver && _player.Health <= 0)
                {
                    return "Поражение. Налетчики прорвались. Очки: " + Score + ". R - рестарт.";
                }
                if (_gameOver)
                {
                    return "Победа. Вся волна уничтожена. Очки: " + Score + ". R - рестарт.";
                }
                return "HP: " + _player.Health + " | Очки: " + Score + " | Враги: " + AliveEnemies().Count;
            }
        }

        public void MovePlayer(int dx)
        {
            if (!_gameOver)
            {
                _player.Move(dx);
            }
        }

        public void PlayerShoot()
        {
            if (_gameOver)
            {
                return;
            }

            Bullet bullet = _player.TryShoot();
            if (bullet != null)
            {
                _bullets.Add(bullet);
            }
        }

        public void Update()
        {
            if (_gameOver)
            {
                return;
            }

            _player.Update();
            _fleet.Update();

            for (int i = _bullets.Count - 1; i >= 0; i--)
            {
                _bullets[i].Update();
                if (!_bullets[i].IsActive)
                {
                    _bullets.RemoveAt(i);
                }
            }

            ResolveHits();
            ResolveEnemyPressure();

            if (!_fleet.IsAlive)
            {
                _gameOver = true;
                _events.Notify(new GameEvent("finish", "Победа: вся вражеская волна уничтожена."));
            }
        }

        public void Draw(Graphics graphics)
        {
            _background.Draw(graphics, new Rectangle(0, 0, 900, 500));

            using (Pen line = new Pen(Color.FromArgb(70, 100, 130)))
            {
                graphics.DrawLine(line, 0, 486, 900, 486);
            }

            _fleet.Draw(graphics);
            for (int i = 0; i < _bullets.Count; i++)
            {
                _bullets[i].Draw(graphics);
            }
            _player.Draw(graphics);
        }

        public void Restart()
        {
            _player = _factory.CreatePlayer();
            _fleet = _director.CreateFirstWave(_factory);
            _bullets.Clear();
            Score = 0;
            _gameOver = false;
            _events.Notify(new GameEvent("restart", "Игра перезапущена."));
        }

        private void ResolveHits()
        {
            List<EnemyShip> enemies = AliveEnemies();
            for (int b = _bullets.Count - 1; b >= 0; b--)
            {
                Bullet bullet = _bullets[b];
                for (int e = 0; e < enemies.Count; e++)
                {
                    EnemyShip enemy = enemies[e];
                    if (bullet.Bounds.IntersectsWith(enemy.Bounds))
                    {
                        enemy.Destroy();
                        bullet.Deactivate();
                        Score += enemy.Score;
                        _events.Notify(new GameEvent("hit", "Попадание: уничтожен " + enemy.Name + ". +" + enemy.Score + " очков."));
                        break;
                    }
                }
            }
        }

        private void ResolveEnemyPressure()
        {
            List<EnemyShip> enemies = AliveEnemies();
            for (int i = 0; i < enemies.Count; i++)
            {
                EnemyShip enemy = enemies[i];
                if (enemy.Bounds.Bottom >= _player.Bounds.Top || enemy.Bounds.IntersectsWith(_player.Bounds))
                {
                    enemy.Destroy();
                    _player.Damage();
                    _events.Notify(new GameEvent("damage", "Корабль получил урон. Осталось HP: " + _player.Health + "."));
                    if (_player.Health <= 0)
                    {
                        _gameOver = true;
                        _events.Notify(new GameEvent("finish", "Поражение: корабль игрока уничтожен."));
                    }
                    return;
                }
            }
        }

        private List<EnemyShip> AliveEnemies()
        {
            List<EnemyShip> enemies = new List<EnemyShip>();
            _fleet.Collect(enemies);
            return enemies;
        }
    }
}
