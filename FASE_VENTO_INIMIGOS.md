# Vertigo — inimigos da fase do vento completa

A cena [`Fase_vento_completa.unity`](Assets/_Project/Scenes/Fase_vento_completa.unity) usa cinco perfis visuais em 55 instâncias de inimigos. Todos os inimigos parados, incluindo os pequenos, usam `ParadoAzul` nessa cena.

| Instâncias | Perfil | Aparência | Opacidade do contorno |
| --- | --- | --- | --- |
| 8 | [`PequenoAmarelo`](Assets/_Project/Art/PerfisInimigos/PequenoAmarelo.asset) | Cristais em tons de cinza e contorno branco. | 0,042 |
| 27 | [`ParadoAzul`](Assets/_Project/Art/PerfisInimigos/ParadoAzul.asset) | Sprite azul original e contorno azul. | 0,22 |
| 11 | [`MovelLaranja`](Assets/_Project/Art/PerfisInimigos/MovelLaranja.asset) | Cristais e contorno azuis. | 0,147 |
| 7 | [`GrandeVermelho`](Assets/_Project/Art/PerfisInimigos/GrandeVermelho.asset) | Cristais e contorno vermelhos. | 0,204 |
| 2 | [`PerseguidorArcoIris`](Assets/_Project/Art/PerfisInimigos/PerseguidorArcoIris.asset) | Cristais e contorno mudam de cor juntos durante o jogo, a 0,32 ciclo por segundo. | 0,063 |

## Reutilizar e ajustar um perfil

1. Selecione o inimigo na Hierarchy e atribua um dos arquivos de `Assets/_Project/Art/PerfisInimigos` ao campo **Cor Do Cristal → Perfil**.
2. Para ajustar todos os inimigos que compartilham um perfil, selecione o arquivo `.asset` na janela Project. **Cor** define a cor do contorno e dos cristais. **Recolorir Sprite** troca os tons azuis e ciano em todos os quadros da animação. Com essa opção desmarcada, só o contorno usa a cor do perfil.
3. Ajuste **Opacidade Contorno** entre `0` e `1`. Para animar a cor, marque **Arco Iris** e ajuste **Ciclos Por Segundo**. Use `0,4` para um ciclo a cada `2,5 s`, `0,8` para um ciclo a cada `1,25 s` ou `1,6` para um ciclo a cada `0,625 s`. O valor `0` pausa a troca na cor inicial.

No arco-íris, o campo **Cor** também define a saturação e o brilho de todo o ciclo. O perfil do perseguidor usa cerca de 55% de saturação e 100% de brilho, com um ciclo a cada 3,125 s.

Para criar uma variação para um único inimigo, duplique o perfil e atribua a cópia à instância. O campo **Perfil** vazio reativa os controles individuais de **Cor Do Cristal** e **Contorno Perigo**.

## Prefabs do inimigo pequeno

Arraste um dos prefabs de `Assets/_Project/Prefabs` para a cena.

- [`InimigoPequeno`](Assets/_Project/Prefabs/InimigoPequeno.prefab) fica no ponto onde foi colocado. Ao ser capturado, lança o jogador na vertical.
- [`InimigoPequenoMovel`](Assets/_Project/Prefabs/InimigoPequenoMovel.prefab) começa com uma patrulha horizontal de `1,3` unidade e mantém o impulso lateral na captura. Para adaptar a patrulha ao local, ajuste **Pontos**, **Velocidade**, **Progresso Inicial** e **Pausa Nos Extremos** no componente **Inimigo Movel** da instância. Os pontos são relativos à posição do objeto na cena.

Os dois usam o perfil `PequenoAmarelo`, escala de aproximadamente `0,54` e velocidade vertical de `11,5` unidades por segundo ao serem capturados. O impulso vertical padrão do jogador na fase é de pelo menos `17` unidades por segundo.

## Prefab do perseguidor

O prefab [`InimigoMovelPercepcao`](Assets/_Project/Prefabs/InimigoMovelPercepcao.prefab) já vem com o perfil arco-íris. Ele é letal ao toque e tem **Block Hook** ligado para impedir que o gancho se prenda nele.

A patrulha começa quando o jogador entra na área de percepção. A rota continua depois que o jogador sai da área. Ao reiniciar, o inimigo volta ao início da rota e espera uma nova entrada. Na seção **Percepção** de **Inimigo Movel**, ajuste os seguintes campos.

| Campo | Efeito |
| --- | --- |
| **Raio Percepcao** | Alcance horizontal da elipse. Com `0`, a patrulha começa imediatamente. |
| **Raio Vertical Percepcao** | Alcance vertical da elipse. Com `0`, usa o mesmo alcance da horizontal. |
| **Deslocamento Percepcao** | Move o centro da área em X e Y, em unidades do mundo. |

O prefab usa raios horizontal e vertical de `6` e `4`. Cada instância pode ajustar sua área e sua rota. Selecione o inimigo na Scene View para ver a elipse.
