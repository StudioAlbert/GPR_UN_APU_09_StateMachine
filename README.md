# GPR-UN-APU-09 — State, un pattern de gameplay

Companion de la séance **GPR-UN-APU-09** (Unity, SAE Institute Genève) : quatre démos
techniques d'une même machine à états générique. Ce ne sont pas des jeux — des primitives,
un panneau de debug qui affiche l'état courant, et rien de plus, pour que le code de la
machine reste lisible.

Unity **6000.3.25f1**, URP, Input System.

## La machine

`Assets/03 - Scripts/Core/` contient la machine générique, **reprise telle quelle** de
[MallLifeStealthGame](https://github.com/StudioAlbert/MallLifeStealthGame/tree/main/MallLife-UnityProject/Assets/03%20-%20Scripts/Core/StateMachine) :

- `StateMachineTypes.cs` — `IState` (`OnEnter` / `OnExit` / `Tick(deltaTime)`) et `StateTransition`
- `StateMachine.cs` — `ChangeState`, `AddTransition`, `AddAnyTransition`, `Tick`

Les transitions sont **déclarées une fois** à côté du graphe, au lieu d'être écrites dans
les états :

```csharp
_machine.ChangeState(idle);
_machine.AddTransition(idle, patrol, AttenteFinie);
_machine.AddTransition(patrol, idle, PointAtteint);
_machine.AddAnyTransition(alarm, JoueurVu);   // depuis n'importe quel état
```

Deux points à savoir avant de lire les démos :

- les transitions sont indexées par `GetType()`, donc **deux états de la même classe
  partagent leurs transitions** — chaque état a sa propre classe, même quand deux états
  font la même chose ;
- `Tick` prend **la première** condition vraie, les « any » d'abord : l'ordre de
  déclaration est l'ordre de priorité.

## Les quatre scènes

`Assets/01 - Scenes/`, à ouvrir dans cet ordre :

| Scène | États | Ce qu'elle montre |
|---|---|---|
| `01 - Hero` | Grounded, Jumping, Landing | `OnEnter` lance une coroutine, la transition attend son drapeau |
| `02 - Guard` | Idle, Patrolling, OnAlarm | `AddAnyTransition` — l'alarme coupe tout |
| `03 - QTE` | Invite, Confirm, Fail, Success, Closing | un état = un panneau d'UI, les boutons sont les transitions |
| `04 - TurnBased` | PlayerTurn, AiTurn, GameOver + Waiting, Active, Empty | deux machines emboîtées, reliées par `OnEnter` |

Commandes : **Espace** (saut), **A** / **R** (alarme), et les boutons du Canvas pour le QTE
et le tour par tour.

## Le panneau de debug

`StateMachineDebugPanel` (IMGUI) liste chaque machine de la scène et son état courant. Il
trouve ses sources tout seul : une démo n'a qu'à implémenter `IStateMachineProbe`
(`MachineName`, `CurrentState`, `Details`). Rien à câbler quand on ajoute une scène.

## Supports de la séance

- Slides : `01 courses/slides/Unity/GPR-UN-APU-09 - …`
- Exercices : `01 courses/exercises/Unity/GPR-UN-APU-09 - …`
