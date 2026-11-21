# Vertigo — inimigos da fase do vento completa

## Prefab do perseguidor

O prefab [`InimigoMovelPercepcao`](Assets/_Project/Prefabs/InimigoMovelPercepcao.prefab) é letal ao toque e tem **Block Hook** ligado para impedir que o gancho se prenda nele.

A patrulha começa quando o jogador entra na área de percepção. A rota continua depois que o jogador sai da área. Ao reiniciar, o inimigo volta ao início da rota e espera uma nova entrada. Na seção **Percepção** de **Inimigo Movel**, ajuste os seguintes campos.

| Campo | Efeito |
| --- | --- |
| **Raio Percepcao** | Alcance horizontal da elipse. Com `0`, a patrulha começa imediatamente. |
| **Raio Vertical Percepcao** | Alcance vertical da elipse. Com `0`, usa o mesmo alcance da horizontal. |
| **Deslocamento Percepcao** | Move o centro da área em X e Y, em unidades do mundo. |

O prefab usa raios horizontal e vertical de `6` e `4`. Cada instância pode ajustar sua área e sua rota. Selecione o inimigo na Scene View para ver a elipse.
