# Vertigo — Fase_Vento 2

A fase foi compactada em três câmaras depois da subida inicial. A rota combina capturas rápidas, mudança de direção no ar e dois usos obrigatórios do vento. O objetivo de ritmo é cerca de 30 segundos em uma passagem limpa.

**Cena:** `Assets/_Project/Scenes/Fase_vento2.unity`. Marcos já começa com o gancho. A fogueira no piso de Y 20 é o checkpoint e a cena está habilitada no build para o reinício.

## Percurso

| Trecho | Ação esperada |
| --- | --- |
| Base e subida, Y 1–33 | Encadeie os inimigos sobre os espinhos, use os pontos fixos para esperar as patrulhas e passe pela fogueira. A subida termina no patamar da corrente vertical. |
| F01 — Poço da corrente, Y 33–51 | Pule para dentro da coluna de vento. Assim que o inimigo aparece à esquerda, use o gancho para sair da corrente antes do teto de espinhos. Faça as capturas seguintes no ar, incluindo uma puxada para baixo sob a laje. |
| F02 — Contramão, Y 51–68 | Após a segunda captura, mire de volta em um inimigo mais alto. O novo impulso permite passar sobre a coluna de espinhos. Continuar em frente cedo leva à lateral da coluna. |
| F03 — Jato e captura, Y 68–81 | Pendure-se no ponto fixo e pule para a esquerda. Pegue a rajada acima da fileira de espinhos e mire no móvel quando ele entrar no alcance. Fora do jato, puxe para cima e para a direita, então inverta de novo para pousar no patamar superior. |
| Túnel e saída, Y 81–92 | Pule à esquerda para a plataforma clara. O inimigo acima do salto é a isca. Fisgá-lo leva à rajada de retorno, que empurra para a direita até um patamar de onde se pode refazer o salto. |

## Referências de montagem

- **Corrente ascendente.** A boca fica em X 11,55 e Y 35,25, acima da extensão direita do patamar. A área vai de X 10,10 a 13,00 e Y 35,25 a 41,65. O primeiro inimigo está em `(7, 43)`. Deixe visível a saída lateral ao subir e conserve o teto perigoso acima da corrente.
- **Jato lateral.** O ponto fixo está em `(1, 74.75)`. A rajada vai para a esquerda entre X −11,05 e 0,10, de Y 73,10 a 77,80. O alvo móvel está em X −15,35. O alvo seguinte fica fora da faixa, em torno de `(-12.05, 77.43)`, para a puxada de retorno não recolocar Marcos no jato.
- **Espinhos.** Os grupos de perigo usam o prefixo `spike_group`, que o `PlayerController` consulta ao detectar colisões. As plataformas cercadas usam tiles separados para tampo, parte de baixo e laterais. Ao mexer no terreno, regenere os colisores dos Tilemaps antes de testar o Play.
- **Componentes.** `WindArea` controla os volumes. `HookTarget`, `InimigoMovel` e `RenascimentoInimigo` controlam as capturas e a reposição dos alvos. O túnel final usa o `GoalZone` existente na plataforma clara.

## Verificação em Play Mode

Uma passagem limpa da partida à saída levou **30,49 s em tempo real** e **28,49 s no relógio da simulação**, usando entradas simuladas de teclado e mouse. O tempo real inclui a câmera lenta durante a mira.

Em testes isolados com as correntes desligadas, o pulo de F01 ficou fora do alcance do primeiro inimigo, e o dash de F03 caiu nos espinhos antes de alcançar a patrulha. O teste da isca confirmou o desvio, o retorno pelo patamar e a chegada à saída.

- Teste a rota toda a partir da partida com a preferência local `Gancho liberado` desligada. Cronometre até o gatilho da saída.
- Teste as duas correntes desligadas individualmente. A primeira captura de F01 deve ficar fora do alcance do pulo. Em F03, a travessia sem jato deve cair nos espinhos antes de alcançar a patrulha.
- Confirme o reinício na fogueira com o gancho. No topo, teste a trajetória direta do pulo e a recuperação após fisgar a isca.
