# Monetização: assista para testar, conquiste ou compre para ter

## As 3 regras

1. **Nenhum anúncio aparece sozinho.** Não existe intersticial nem banner. O vídeo só toca quando o jogador
   toca num botão "assistir".
2. **Anúncio só dá vantagem TEMPORÁRIA.** Boosters, roupa em teste, item ou skill com níveis extras por alguns
   minutos. Quando o tempo acaba, tudo volta ao que era.
3. **Posse permanente só por conquista ou compra.** Moedas, roupas, níveis de item, níveis de skill e tickets
   nunca vêm de anúncio. Vêm de jogar (quests, rank, tocar, idle) ou de comprar na loja.

Diferente do original, que mostra intersticial a cada pausa e dá moedas e upgrades por vídeo.

## Como as regras são garantidas no código

| Camada | Garantia |
|---|---|
| `RewardRules` (`Economy/Rewards.cs`) | Lista o que é temporário. Permanente só com origem `Gameplay` ou `Purchase` |
| `RewardGranter` | Valida o lote inteiro antes de aplicar. Se um anúncio tentar dar algo permanente, lança `RewardPolicyException` e nada é entregue |
| `MonetizationConfigValidator` | No startup, recusa o `ad_placements.json` se algum placement der algo permanente ou não tiver duração |
| `RewardedOfferService` | Recusa placements não temporários já no construtor |
| `TemporaryEffects` | Guarda os efeitos com prazo no save. O gameplay **consulta** os efeitos e nunca altera nível ou posse real por causa deles |
| `IAdService` | Só tem `ShowRewarded`. Não existe API de intersticial ou banner para alguém chamar por engano |

## Assistir e ganhar temporariamente (`ad_placements.json`)

| placementId | O que o jogador ganha | Duração | Cooldown |
|---|---|---|---|
| `rv_booster_tap_x4` | Toque ×4 | 3 min | — |
| `rv_booster_pass_x4` | Renda passiva ×4 | 3 min | — |
| `rv_booster_income_x10` | Renda ×10 | 1 min | 5 min |
| `rv_booster_autoclick` | Autoclique | 5 min | — |
| `rv_booster_double_coin` | Moedas ×2 por toque | 5 min | — |
| `rv_outfit_trial` | **Vestir uma roupa bloqueada**, com o boost dela | 30 min | 10 min |
| `rv_item_trial` | **Item escolhido +10 níveis** | 15 min | 10 min |
| `rv_skill_trial` | **Skill escolhida +5 níveis** | 10 min | 10 min |
| `rv_pk_retry` | Uma revanche na Batalha de Horda | vale por 2 min | — |

- As moedas ganhas **durante** um booster são conquistadas pelo jogador, tocando ou no idle. O anúncio só acelera por um tempo.
- **Ticket:** quem comprou tickets pode gastar 1 no lugar do vídeo e recebe o mesmo prêmio temporário.
- Boosters iguais acumulam tempo (assistir 2× o x4 Toque dá 6 min).
- Só uma roupa em teste por vez.

### Quando o teste acaba

`TemporaryEffects.Expired` dispara e a UI mostra: *"Gostou da roupa? Conquiste por X moedas ou compre agora"*.
O botão "comprar" abre `zs_outfit_unlock` com a roupa testada. Para item testado, o mesmo vale com `zs_item_levels_10`.
É aqui que o teste vira venda, sem dar nada de graça.

## Conquistar jogando (origem `Gameplay`)

Tocar, renda idle, doações do chat, quests, subir de rank, presente AFK (só o ganho real, **sem** multiplicador por vídeo),
vitória no PK e comprar itens, níveis e roupas com moedas do jogo.
O código do gameplay chama `RewardGranter.Grant(..., RewardSource.Gameplay)`.

## Comprar (`iap_catalogo.json`)

| productId | Tipo | Conteúdo | Preço sugerido |
|---|---|---|---|
| `zs_tickets_5` … `zs_tickets_300` | consumível | 5 / 15 / 35 / 80 / 140 / 300 tickets (pulam o vídeo) | US$ 0,99 – 24,99 |
| `zs_coins_small` | consumível | 2 h de renda atual | US$ 0,99 |
| `zs_coins_big` | consumível | 12 h de renda atual | US$ 4,99 |
| `zs_income_x2` | não consumível | **Renda ×2 para sempre** | US$ 6,99 |
| `zs_golden_pack` | consumível, 1× por conta | 24 h de renda + 30 tickets + roupa índice 9 | US$ 9,99 |
| `zs_outfit_unlock` | consumível | Compra a roupa escolhida (oferecida ao fim do teste) | US$ 2,99 |
| `zs_item_levels_10` | consumível | +10 níveis permanentes no item escolhido | US$ 1,99 |

Não existe mais "Remove Ads", porque não há anúncio forçado para remover.

**Fluxo de compra:** `Buy(produto, alvo)` → salva o alvo escolhido → loja → compra validada → entrega uma vez por
transação → **salva** → confirma à loja. Se a entrega ou o save falharem, a compra fica pendente e a loja reenvia.
O Google Play reembolsa sozinho o que não for confirmado em 3 dias.

- **Produto com escolha** (`zs_outfit_unlock`, `zs_item_levels_10`): a escolha é salva antes de abrir a loja e descartada se
  a compra for cancelada. Se ela se perder (save apagado entre a compra e a entrega), a compra fica pendente sem ser
  confirmada e o Google Play reembolsa em 3 dias. Uma melhoria futura é a UI perguntar o alvo de novo.
- **Robustez:** um vídeo por vez (trava global), ticket devolvido se o prêmio falhar, cooldown só conta se o prêmio foi
  entregue, valores numéricos inválidos recusados antes de aplicar qualquer item, e o save é garantido antes de confirmar
  até no reenvio da loja.

## Uso na UI

```csharp
// Botão "x4 Toque" (ícone de ticket se o jogador tiver tickets)
ticketIcon.SetActive(offers.WillUseTicket("rv_booster_tap_x4"));
offers.Claim("rv_booster_tap_x4", outcome => ShowToast(outcome));

// "Provar esta roupa por 30 min"
offers.Claim("rv_outfit_trial", Done, outfitIndex: roupa.Index);

// Fim do teste → oferta
effects.Expired += e => { if (e.kind == EffectKind.OutfitTrial) ShowBuyOrEarn(int.Parse(e.key)); };
purchases.Buy("zs_outfit_unlock", outfitIndex: roupaTestada);

// Cálculo de renda no gameplay
var nivelEfetivo = nivelReal + effects.BonusLevels(EffectKind.ItemTrial, "bunker:4");
var mult = wallet.PermanentIncomeMultiplier * (effects.IsBoosterActive(BoosterType.PassiveX4) ? 4 : 1);
var roupaVisivel = effects.ActiveOutfitTrial() >= 0 ? effects.ActiveOutfitTrial() : roupaEquipada;
```

## Conformidade

- **Consentimento:** Google UMP (GDPR/LGPD) antes de inicializar o MAX.
- **Política do Play:** rewarded sempre opcional e com o prêmio dito antes do vídeo. Sem intersticial, o maior risco
  de política deixa de existir.
- **Families:** se o público incluir menores, use só redes certificadas e sem anúncio personalizado.
- **Save na nuvem:** sem ele, reinstalar num aparelho novo perde o registro de compras de "1× por conta".
  Os não consumíveis (`zs_income_x2`) voltam pelo restore da loja.
- **Relógio:** efeitos e cooldowns usam o relógio do aparelho. Para algo valioso, use hora do servidor.
- **Chaves:** SDK key e ad unit Rewarded do MAX vêm do Inspector ou Remote Config, nunca de texto versionado.
