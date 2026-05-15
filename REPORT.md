# Отчет по лабораторным работам 7-8

## Тема

Разработка работающей игры на статически типизированном языке программирования с применением паттернов проектирования, изученных в лабораторных работах 1-6.

## Цель работы

Разработать пошаговую стратегию на C#, применив несколько паттернов проектирования в комплексе, подготовить UML-диаграммы классов и последовательности, а также распределить работы между участниками команды.

## Выбранная игра

Разработана оконная пошаговая стратегия `Pattern Quest` на WinForms.

Игрок управляет армией фракции `Орден Севера` и сражается с армией `Налетчики Пустоши`. Армии состоят из отрядов: авангард, дальний ряд, поддержка. Игрок использует кнопки окна, а внутри программы эти действия преобразуются в команды мини-языка:

- `scout` - разведка карты;
- `attack <номер_своего> <номер_врага>` - атака;
- `heal <номер_своего>` - лечение союзника;
- `end` - завершение боя.

Исходный код расположен в файле [Program.cs](C:/Users/roman/pin12-VM/Dev/game/PatternQuest/Program.cs).

## Примененные паттерны из лабораторных работ 1-6

| ЛР | Паттерн | Реализация в проекте |
| --- | --- | --- |
| 1 | Singleton | `Logger` - единый журнал игровых сообщений. |
| 1 | Abstract Factory | `IFactionFactory`, `OrderFactory`, `RaidersFactory` создают совместимые семейства юнитов и карту фракции. |
| 2 | Builder | `ArmyBuilder` и `ArmyDirector` собирают армию из отрядов. |
| 3 | Composite | `IArmyComponent`, `Unit`, `Squad` позволяют одинаково работать с отдельным юнитом и составным отрядом. |
| 4 | Proxy | `MapProxy` откладывает создание `RealMapImage` до команды разведки. |
| 5 | Interpreter | `CommandParser` и выражения `AttackExpression`, `HealExpression`, `ScoutExpression`, `EndExpression` интерпретируют команды игрока. |
| 6 | Observer | `EventBus`, `BattleJournal`, `ScoreBoard` получают уведомления о ходе боя. |

## Диаграмма классов

```mermaid
classDiagram
    class Logger {
        -Logger instance
        +Instance Logger
        +Write(message)
    }

    class IFactionFactory {
        <<interface>>
        +FactionName
        +CreateInfantry() Unit
        +CreateArcher() Unit
        +CreateSupport() Unit
        +CreateMap() IMapImage
    }
    class OrderFactory
    class RaidersFactory
    IFactionFactory <|.. OrderFactory
    IFactionFactory <|.. RaidersFactory

    class IArmyComponent {
        <<interface>>
        +Name
        +Power
        +IsAlive
        +Print(indent)
    }
    class Unit {
        +Health
        +Attack
        +Healing
        +TakeDamage(damage)
        +Heal(target)
    }
    class Squad {
        -children
        +Add(component)
        +Units()
    }
    IArmyComponent <|.. Unit
    IArmyComponent <|.. Squad
    Squad o-- IArmyComponent

    class ArmyBuilder {
        +AddVanguard()
        +AddRangeLine()
        +AddSupport()
        +Build() Squad
    }
    class ArmyDirector {
        +CreateBalancedArmy(factory) Squad
    }
    ArmyBuilder --> IFactionFactory
    ArmyDirector --> ArmyBuilder

    class IMapImage {
        <<interface>>
        +DrawBox()
        +Reveal()
    }
    class MapProxy
    class RealMapImage
    IMapImage <|.. MapProxy
    IMapImage <|.. RealMapImage
    MapProxy --> RealMapImage

    class ICommandExpression {
        <<interface>>
        +Interpret(context)
    }
    class CommandParser {
        +Parse(line) ICommandExpression
    }
    class AttackExpression
    class HealExpression
    class ScoutExpression
    class EndExpression
    ICommandExpression <|.. AttackExpression
    ICommandExpression <|.. HealExpression
    ICommandExpression <|.. ScoutExpression
    ICommandExpression <|.. EndExpression
    CommandParser --> ICommandExpression

    class EventBus {
        +Attach(observer)
        +Notify(event)
    }
    class IGameObserver {
        <<interface>>
        +Update(event)
    }
    class BattleJournal
    class ScoreBoard
    IGameObserver <|.. BattleJournal
    IGameObserver <|.. ScoreBoard
    EventBus --> IGameObserver

    class GameContext {
        +Attack(attackerIndex, targetIndex)
        +Heal(targetIndex)
        +Scout()
        +EndBattle()
    }
    class Game {
        +CreateDemo()
        +RunScript(commands)
        +RunInteractive()
    }
    Game --> GameContext
    GameContext --> Squad
    GameContext --> IMapImage
    GameContext --> EventBus
```

## Диаграмма последовательности

```mermaid
sequenceDiagram
    actor Player as Игрок
    participant Game as Game
    participant Parser as CommandParser
    participant Expr as ICommandExpression
    participant Ctx as GameContext
    participant Map as MapProxy
    participant Bus as EventBus
    participant Journal as BattleJournal
    participant Score as ScoreBoard

    Player->>Game: вводит "scout"
    Game->>Parser: Parse(command)
    Parser-->>Game: ScoutExpression
    Game->>Expr: Interpret(context)
    Expr->>Ctx: Scout()
    Ctx->>Map: Reveal()
    Map->>Map: создает RealMapImage при первом вызове
    Ctx->>Bus: Notify(GameEvent)
    Bus->>Journal: Update(event)
    Bus->>Score: Update(event)

    Player->>Game: вводит "attack 0 0"
    Game->>Parser: Parse(command)
    Parser-->>Game: AttackExpression
    Game->>Expr: Interpret(context)
    Expr->>Ctx: Attack(0, 0)
    Ctx->>Bus: Notify(attack)
    Bus->>Journal: Update(event)
    Bus->>Score: Update(event)
```

## Распределение ролей в команде из 3 человек

| Участник | Роль | Что выполнял |
| --- | --- | --- |
| Рома | Архитектор и аналитик | Изучил задания лабораторных 1-8, выбрал жанр пошаговой стратегии, описал игровые сущности, распределил паттерны по зонам ответственности, подготовил UML-диаграмму классов. |
| Люда | Разработчик игровой логики | Реализовала C#-код игры: создание фракций, сборку армий, составные отряды, расчет атаки и лечения, обработку победы и демонстрационный сценарий. |
| Снежана | Разработчик интерфейса и тестировщик | Реализовала интерпретатор команд игрока, журнал событий, счетчик действий, проверила запуск игры, подготовила результат выполнения и оформила отчет. |

## Результат выполнения программы

Команда сборки оконной версии на текущей машине:

```powershell
C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe /nologo /target:winexe /r:System.Windows.Forms.dll /r:System.Drawing.dll /out:PatternQuest\PatternQuestWin.exe PatternQuest\Program.cs
```

Команда запуска:

```powershell
.\PatternQuest\PatternQuestWin.exe
```

Результат работы: открывается модальное окно `Pattern Quest - лабораторные 7-8` с двумя списками армий, полем состояния карты, кнопками `Разведка`, `Атаковать`, `Лечить`, `Завершить бой` и журналом событий.

Фрагмент журнала:

```text
[LOG] Загрузка реальной карты snow-pass.map
Разведка открыла карту и позиции противника.
> attack 0 0
Страж атакует Берсерк на 8 урона.
Ответный ход: Берсерк атакует Страж.
> heal 2
Целитель лечит Арбалетчик.
> end
Бой завершен по команде игрока.
```

## Вывод

В ходе лабораторных работ 7-8 была разработана работающая оконная пошаговая стратегия на C#. В проекте применены паттерны из лабораторных работ 1-6: Singleton, Abstract Factory, Builder, Composite, Proxy, Interpreter и Observer. Использование паттернов позволило разделить создание объектов, сборку армии, структуру отрядов, обработку команд, ленивую загрузку карты и рассылку игровых событий.
