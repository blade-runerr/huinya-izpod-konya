# Pattern Quest Shooter

Оконная аркадная стрелялка на C# WinForms для лабораторных работ 7-8.

## Запуск

Если установлен .NET SDK:

```powershell
dotnet run --project .\PatternQuest\PatternQuest.csproj
```

На текущей машине проект проверяется через системный компилятор .NET Framework:

```powershell
C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe /nologo /target:winexe /r:System.Windows.Forms.dll /r:System.Drawing.dll /out:PatternQuest\PatternShooter.exe PatternQuest\Program.cs
.\PatternQuest\PatternShooter.exe
```

В Git Bash запускать так:

```bash
./PatternQuest/PatternShooter.exe
```

## Управление

- `A` или стрелка влево - движение влево.
- `D` или стрелка вправо - движение вправо.
- `Space` - выстрел.
- `R` - рестарт после победы или поражения.

## Правила

Игрок управляет синим кораблем внизу экрана. Враги двигаются сверху и постепенно опускаются. Нужно стрелять по ним и уничтожить всю волну.

Победа: все враги уничтожены.

Поражение: здоровье игрока стало равно 0.

В игре сохранены паттерны из лабораторных 1-6: Singleton, Abstract Factory, Builder, Composite, Proxy, Interpreter и Observer.
