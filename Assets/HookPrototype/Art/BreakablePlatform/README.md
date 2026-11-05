# Plataforma quebravel

Arraste `Assets/HookPrototype/Prefabs/BreakableRockPlatform.prefab` para a cena.
O pivo fica no centro da superficie superior: posicione-o na altura onde o jogador deve pisar.
O prefab mede 1,8 unidades de largura; pode ser redimensionado pelo Transform.

- Contato do PlayerController por cima inicia uma contagem unica de 2 segundos.
- Sair da plataforma nao interrompe nem reinicia a contagem.
- Visual inteiro ate aproximadamente 0,67 s; rachado ate 1,33 s; muito rachado ate 2 s.
- Ao terminar, a colisao e o visual sao desativados e o objeto e destruido.
- Usa tempo de jogo: pausa e camera lenta tambem afetam a contagem.
- Recarregar a cena restaura as instancias, como os outros elementos do prototipo.
- `Break Delay` e os sprites podem ser ajustados no componente Breakable Platform.
- Os cantos e as laterais nao tem atrito (PlatformEffector2D, Side Arc 160 graus). A superficie superior mantem o atrito original; a plataforma continua solida por baixo.

O marcador PrototypePlatform ja esta no prefab para manter salto, grounded e bloqueio do gancho compativeis com PlayerController. Nao e necessario alterar o jogador.

## Arte

Variacao do sprite rockDecor_6 em Assets/HookPrototype/Art/tileset/rockDecor.png.
Atlas original preservado. Os tres novos PNGs tem 18 x 16 pixels, filtro Point, compressao desativada e pivo superior identico. A pedra foi invertida, com a ponta para baixo e a neve na superficie larga superior.

Imagem criada com a ferramenta integrada image_gen, usando um recorte ampliado do sprite original como referencia. O resultado foi dividido em tres quadros e reduzido por nearest neighbor para a resolucao do sprite de origem.

Prompt usado na geracao:

Use case: precise-object-edit. Asset type: transparent pixel-art game sprite sheet, three animation frames of ONE breakable rock platform. Input image is an enlarged exact 18 by 16 pixel sprite from this game's tileset, and is the edit target. Preserve its very simple coarse pixel style, muted dark-purple rock palette and pale icy white/green snow palette. Primary edit: vertically invert the triangular pointed rock so it has a broad FLAT HORIZONTAL TOP and tapers to a point hanging DOWN. Remove ALL snow from the downward point, and relocate that snow into a continuous flat thin snowy cap on the broad TOP surface that the player can stand on. Generate one horizontal sprite sheet with exactly THREE equally sized square cells, aligned left to right, transparent background. Each cell contains exactly the SAME rock platform at the SAME position and scale, with identical silhouette, same flat snow cap height and bottom point. Left cell: intact rock. Middle cell: same rock with a few clearly visible dark pixel cracks beginning from the snowy top. Right cell: same rock with larger branching dark pixel cracks across the rock face and a few small gaps in snow, but the platform still retains same silhouette until it disappears in the game. Keep rock geometry very simple like the original; roughly 18 coarse pixels wide by 16 coarse pixels high, enlarged with hard square edges and nearest-neighbor appearance. Fill each cell with the sprite leaving a narrow, equal transparent margin, point centered. No detached particles, no debris, no ground, no shadows, no text, no numbers, no borders, no labels, no perspective, no smooth gradients, no antialiased painted texture, no extra platforms. Output wide 3:1 sheet if possible, exactly equal cells suitable for Unity slicing.