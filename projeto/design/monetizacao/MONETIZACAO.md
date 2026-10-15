# Monetização: ganhar itens comprando ou assistindo anúncios

Modelo híbrido, igual ao original: **IAA** (anúncios) + **IAP** (compras). Toda recompensa, venha de onde vier, passa
pelo mesmo serviço (`RewardGranter`). Por isso uma recompensa nova é só uma linha de JSON.

```
          ┌──────────── iap_catalogo.json ───────────┐     ┌────────── ad_placements.json ──────────┐
          │ productId → [recompensas]                │     │ placementId → recompensa + regras      │
          └──────────────────┬───────────────────────┘     └──────────────────┬─────────────────────┘
 Loja (Unity IAP) ──► PurchaseRewardHandler                     RewardedOfferService ◄── botão na UI
                      • idempotente por transactionId           • tem ticket? gasta 1 ticket
                      • confirma à loja só após entregar         • senão mostra vídeo (AppLovin MAX)
                      └──────────────┐                ┌─────────• cooldown por placement
                                     ▼                ▼
                                   RewardGranter ──► PlayerWallet (moedas, tickets, no-ads, roupas)
                                                 └─► IGameplayRewardSink (boosters, upgrades, PK)
 InterstitialPacer: intersticial só em pausa natural, com intervalo mínimo, limite por sessão e nunca para quem tem no-ads.
```

## 1. Ganhar com anúncios (rewarded, sempre opcional)

| placementId | Botão | Recompensa | Ticket pula? |
|---|---|---|---|
| `rv_booster_tap_x4` | x4 Toque | Toque ×4 por 180 s | sim |
| `rv_booster_pass_x4` | x4 Passivo | Renda passiva ×4 por 180 s | sim |
| `rv_booster_income_x10` | x10 Renda | Renda ×10 por 60 s | sim |
| `rv_booster_autoclick` | Autoclique | Autoclique por 300 s | sim |
| `rv_booster_double_coin` | Moeda x2 | Moedas ×2 por 300 s | sim |
| `rv_afk_multiplier` | Presente AFK | Roleta ×2 / ×3 / ×5 sobre o ganho offline | sim |
| `rv_donation_x2` | Doação x2 | Dobra a doação coletada | sim |
| `rv_free_gift` | Presente grátis | 30 min de renda (cooldown de 10 min) | sim |
| `rv_upgrade_skill_free` | Upgrade grátis | +1 nível de skill | sim |
| `rv_rankup_bonus` | Subiu de rank | Recompensa do rank ×3 | sim |
| `rv_pk_retry` | Perdeu a batalha | Tentar de novo | sim |

**Intersticial** (`is_natural_break`): só em pausas naturais (fechar loja, trocar de sala, fim de história ou PK), 90 s entre
intersticiais, 60 s depois de um rewarded, nenhum nos primeiros 5 min após instalar e no máximo 8 por sessão. Mostre o aviso
"Intervalo comercial" antes, como o "Ad break" do original. **Banner** fica embaixo, escondido para quem comprou no-ads.

## 2. Ganhar comprando (IAP)

| productId | Tipo | Conteúdo | Preço sugerido |
|---|---|---|---|
| `zs_noads` | não consumível | Remove banner e intersticial (rewarded continua opcional) | US$ 4,99 |
| `zs_noads_tickets` | não consumível | No-ads + 50 tickets (POPULAR) | US$ 7,99 |
| `zs_tickets_5` … `zs_tickets_300` | consumível | 5 / 15 / 35 / 80 / 140 / 300 tickets | US$ 0,99 – 24,99 |
| `zs_coins_small` | consumível | 2 h de renda atual | US$ 0,99 |
| `zs_coins_big` | consumível | 12 h de renda atual | US$ 4,99 |
| `zs_golden_pack` | consumível, 1× por conta | 24 h de renda + 30 tickets + traje índice 9 ("Torcida com Taco") | US$ 9,99 |
| `zs_outfit_discount` | consumível | Próximo traje bloqueado com 15% OFF (oferta relâmpago) | US$ 2,99 |

Moedas que **escalam com a renda** (`coinsScaledToIncome`) mantêm o pacote relevante em qualquer ponto do jogo, como no idle original.
O **ticket** é a ponte entre os dois modelos: quem paga pula vídeos e continua recebendo todas as recompensas.

## 3. Onde fica cada parte do código

| Arquivo | Papel |
|---|---|
| `jogo/Assets/Scripts/Economy/Rewards.cs` | Tipos de recompensa e booster (`RewardDefinition`) |
| `jogo/Assets/Scripts/Economy/PlayerWallet.cs` | Moedas, tickets, no-ads, roupas e transações processadas (save) |
| `jogo/Assets/Scripts/Economy/RewardGranter.cs` | **Único** ponto de crédito |
| `jogo/Assets/Scripts/Monetization/RewardedOfferService.cs` | Fluxo "ticket ou vídeo → recompensa", com cooldown |
| `jogo/Assets/Scripts/Monetization/PurchaseRewardHandler.cs` | Fluxo "compra validada → recompensa → salva → confirma" |
| `jogo/Assets/Scripts/Monetization/InterstitialPacer.cs` | Regras de frequência do intersticial |
| `jogo/Assets/Scripts/Monetization/Adapters/MaxAdService.cs` | AppLovin MAX (define `ZS_APPLOVIN_MAX`) |
| `jogo/Assets/Scripts/Monetization/Adapters/UnityIapService.cs` | Unity IAP (define `ZS_UNITY_IAP`; TODOs da versão v5) |
| `jogo/Assets/Scripts/Monetization/Adapters/MonetizationBootstrap.cs` | Lê os JSON e monta tudo; fakes no Editor |
| `jogo/Assets/Tests/EditMode/MonetizationTests.cs` | Testes de ticket vs. vídeo, cooldown, idempotência e pacing |

Uso na UI:

```csharp
// Botão "x4 Toque"
offers.Claim("rv_booster_tap_x4", outcome => ShowToast(outcome));
// Ícone do botão: ticket ou vídeo
ticketIcon.SetActive(offers.WillUseTicket("rv_booster_tap_x4"));
// Roleta AFK: depois de sortear o multiplicador
offers.Claim("rv_afk_multiplier", Done, placement.reward.WithMultiplier(sorteado), baseAmount: ganhoOffline);
// Loja
purchases.Buy("zs_tickets_35");
```

## 4. Conformidade (antes de publicar)

- **Consentimento:** Google UMP (GDPR/LGPD) antes de inicializar o MAX. O MAX tem fluxo CMP integrado (o original usa `MaxCmpService`).
- **Google Play Families:** se o público incluir menores, use só redes certificadas para Families e não use anúncios personalizados.
  O tema é pensado para 10+, mas confirme a classificação IARC.
- **Política de anúncios do Play:** sem intersticial inesperado no meio da interação, sem recompensa que exija o anúncio
  para progredir e rewarded sempre opcional e claramente sinalizado.
- **Ordem de entrega:** creditar → salvar (`persist`) → confirmar à loja. Se o save falhar, a compra fica pendente e a loja reenvia.
- **Restore em aparelho novo:** sem save na nuvem (Google Play Games Saved Games, Firebase ou servidor próprio), um save vazio
  não sabe que os tickets do `zs_noads_tickets` já foram dados e os entrega de novo. Implemente save na nuvem antes de lançar,
  ou venda tickets só em produtos consumíveis.
- **Cooldowns** (presente grátis) ficam no save, mas usam o relógio do aparelho. Para algo valioso, use hora do servidor.
- **Recibos:** valide no servidor (ou via AppsFlyer/Play Integrity) antes de entregar compras caras. O `transactionId` já evita entrega dupla.
- **Restaurar compras:** botão em Configurações, que chama `IIapService.RestorePurchases`.
- **Chaves:** SDK key e ad units do MAX vêm do Inspector ou Remote Config, nunca de texto versionado.
