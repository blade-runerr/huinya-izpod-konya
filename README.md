# Pattern Quest

Оконная пошаговая стратегия на C# WinForms для лабораторных работ 7-8.

## Запуск

Если установлен .NET SDK:

```powershell
dotnet run --project .\PatternQuest\PatternQuest.csproj
```

На текущей машине проект также проверен через системный компилятор .NET Framework:

```powershell
C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe /nologo /target:winexe /r:System.Windows.Forms.dll /r:System.Drawing.dll /out:PatternQuest\PatternQuestWin.exe PatternQuest\Program.cs
.\PatternQuest\PatternQuestWin.exe
```

В Git Bash запускать так:

```bash
./PatternQuest/PatternQuestWin.exe
```

## Управление

- `Разведка` - открыть карту через proxy.
- `Атаковать` - выбранный свой юнит атакует выбранного врага.
- `Лечить` - целитель лечит выбранного союзника.
- `Завершить бой` - закончить партию.
