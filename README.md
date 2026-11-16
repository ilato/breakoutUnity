# Breakout — Trabalho Individual de Desenvolvimento de Jogos Digitais

Jogo 2D no estilo Breakout/Arkanoid, feito em **Unity 6** (C#).

## Como jogar

| Ação | Controle |
|---|---|
| Mover a raquete | Setas ← → / A D / mouse / analógico |
| Lançar a bola | Espaço / clique esquerdo / botão A |
| Pausar | Esc / P |
| Voltar ao menu (pausado) | M |
| Sair (no menu) | Esc |

Quebre todos os tijolos das **3 fases** sem deixar a bola cair. Você começa com 3 vidas.

- **Tijolos:** coloridos (1 golpe), prata (2 golpes), ouro (3 golpes)
- **Power-ups** (caem de tijolos quebrados): **verde** = raquete maior, **rosa** = vida extra, **azul** = bola lenta
- **Pontuação:** 100 pontos por golpe necessário de cada tijolo, +1000 por fase concluída; o recorde fica salvo

## Como abrir

1. Abra a pasta do projeto no Unity Hub (Unity 6).
2. Na primeira abertura, a cena é montada automaticamente em `Assets/Breakout/Scenes/Breakout.unity`.
   Se precisar montar de novo: menu **Breakout → Montar Jogo**.
3. Aperte **Play**.

## Conceitos de desenvolvimento de jogos aplicados

| Conceito | Onde está |
|---|---|
| Entrada do jogador (teclado, mouse, controle) | `GameInput.cs` (Input System) |
| Movimentação de personagens/objetos | `Paddle.cs` (raquete), `PowerUp.cs`, `Debris.cs` |
| Transform e física | `Rigidbody2D`, `PhysicsMaterial2D` com quique total, `MovePosition` |
| Colisões e interações | `Ball.OnCollisionEnter2D`, `DeathZone` e `PowerUp` (triggers) |
| Pontuação, vidas, vitória/derrota | `GameManager.cs` (recorde salvo com `PlayerPrefs`) |
| Controle de estados | `GameState`: Menu, Saque, Jogando, Pausado, Fase concluída, Fim de jogo, Vitória |
| Interface e feedback | `UIManager.cs` (HUD e mensagens), sons gerados por código (`SoundManager.cs`), tremida de câmera, partículas, rastro da bola, tijolos piscando |
| Prefabs e fases | `LevelManager.cs` instancia o prefab de tijolo a partir de layouts em texto |

## Estrutura

```
Assets/Breakout/
├── Scripts/   # código do jogo
├── Editor/    # BreakoutBuilder.cs — gera sprites, prefabs e a cena
├── Art/       # sprites gerados
├── Prefabs/   # Brick, PowerUp
├── Physics/   # material de física da bola
└── Scenes/    # Breakout.unity
```
