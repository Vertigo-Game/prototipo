# Vertigo — nova Fase 1

O jogo terá três fases. A primeira fica em uma cena nova, `Fase1_Validacao.unity`, montada a partir do desenho do percurso. A cena `Fase1.unity` fica reservada para as fases seguintes. **A Fase 1 inteira acontece sem frio**, inclusive dentro da caverna.

## Percurso

1. **Início e plataformas.** Marcos começa junto da fogueira inicial, com uma placa de tutorial de pulo e dash ao lado. Um degrau, uma plataforma e um pilar ensinam os pulos. Do pilar, um vão pede pulo com dash até a plataforma seguinte.
2. **Descida, checkpoint e gancho.** Da plataforma, Marcos desce ao patamar inferior. Ali ficam a segunda fogueira (checkpoint), a placa da fogueira, a placa do gancho e o gancho. Se morrer depois de pegar o gancho, Marcos volta ao checkpoint com o item já liberado.
3. **Travessia sobre espinhos.** Logo depois do patamar, os buracos entre as plataformas são cobertos de espinhos. O primeiro pouso tem a placa das pedrinhas.
4. **Gancho nas pedras fixas.** Três pedras fixas levam Marcos de plataforma em plataforma, ganhando altura. Cada pedra fica ao alcance do gancho a partir do pouso anterior.
5. **Impulso no inimigo e entrada da caverna.** Na última plataforma, a placa da pedra maior avisa que o inimigo só pode ser tocado com o gancho. A captura dá o impulso até a boca da caverna, sem precisar de dash. Ao atravessá-la, o fundo muda de céu para rocha.
6. **Fogueira final.** Da entrada, Marcos segue a pé pelo piso contínuo até a fogueira final. A placa ao lado diz: "A partir daqui, o frio pode ser cruel."

## Montagem

- Terreno no padrão da `Fase1.unity`: Grid com `ground_collision` (`TilemapCollider2D` + `CompositeCollider2D`), `walls` para o fundo da caverna e `spike_groupo/spikes` para os espinhos.
- A colisão usa `rocktiles1`. A neve do lado de fora fica no Tilemap `neve`, só visual, porque as laterais do `rocktiles2` não têm formato de colisão próprio.
- O tileset não tem peça de canto côncavo. Onde a parede encontra o chão, o canto é montado com meia peça em `ground_collision` e a outra metade em `ground_collision cantos`.
- Céu noturno, parallax, valores de movimento do jogador, câmera, fogueiras, placas e gancho seguem os da `Fase1.unity`.

## Validação no Unity

Abra `Assets/_Project/Scenes/Fase1_Validacao.unity` e aperte Play. Com essa cena aberta, o Play começa direto nela, sem passar pela tela inicial.

No playtest, conferir os pulos e o dash do início, a descida até o checkpoint e o gancho, o retorno ao checkpoint com o gancho depois de uma morte, a travessia das pedras, o impulso do inimigo até a caverna e o caminho a pé até a fogueira final.
