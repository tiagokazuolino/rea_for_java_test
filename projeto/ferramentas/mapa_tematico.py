"""Tabelas de conversão temática: jogo original (streamer) -> "Zombie Streamer".

Cada entrada descreve O QUE o asset é no jogo original (função) e O QUE ele
passa a ser no jogo novo. Os prompts são gerados a partir daqui por
gerar_catalogo.py. Toda personagem é adulta (18+), sem sexualização; zumbis
são "fofos-assustadores", sem gore explícito.
"""

STYLE = (
    "high-quality anime game illustration, soft cel shading, clean lineart, "
    "vibrant saturated colors, glossy highlights, mobile idle game asset, "
    "post-apocalyptic cozy vibe, cute-spooky zombie theme"
)
STYLE_CHARACTER = (
    "adult woman (age 20+), modest non-revealing outfit, full body, "
    "thick white sticker outline around silhouette, transparent background, "
    "centered, no text"
)
STYLE_PROP = (
    "isometric 3/4 view game prop, soft drop shadow, transparent background, "
    "centered, no text, no characters"
)
STYLE_UI = (
    "glossy candy mobile game UI element, rounded shapes, bevel and inner glow, "
    "palette toxic lime green #9BE564 + hot pink #FF4FA3 + deep purple #3B1F5C, "
    "subtle duct-tape and scratch details, transparent background, flat front view"
)
STYLE_ICON = (
    "glossy 3D-style game icon, bold outline, transparent background, centered, "
    "readable at 64px, no text unless specified"
)
STYLE_BG = (
    "anime background painting, wide interior/exterior scene, no characters, "
    "depth of field, warm and neon lighting mixed, post-apocalyptic but cozy"
)
NEGATIVE = (
    "nsfw, nudity, cleavage, lingerie, swimsuit, suggestive pose, child, loli, "
    "gore, blood splatter, exposed organs, realistic photo, 3d render, text, "
    "watermark, logo, signature, blurry, jpeg artifacts, extra fingers, "
    "deformed hands, cropped"
)

GAME_TITLE = "Zombie Streamer: Idle Survival"

# ---- Elenco -------------------------------------------------------------
# prefixo do sprite original -> (função original, nova personagem, descrição visual)
CHARACTERS = {
    "main_bikini": ("Protagonista - traje de praia (sala 3)", "Mia - traje de praia",
                    "Mia, cheerful survivor streamer, blonde hair with blue tips, gaming headset, "
                    "modest one-piece-style sports top with board shorts and open hoodie, "
                    "life vest, holding a coconut drink and a walkie-talkie"),
    "main_hs": ("Protagonista - uniforme escolar (sala 2)", "Mia - uniforme da escola abandonada",
                "Mia, blonde hair with blue tips, gaming headset, school blazer with red bow, "
                "knee pads and a backpack full of scavenged supplies"),
    "main_pv": ("Protagonista - uniforme de trabalho (sala 4/5)", "Mia - uniforme de abrigo",
                "Mia, blonde hair with blue tips, gaming headset, navy vest uniform with name tag, "
                "tool belt with flashlight"),
    "main": ("Protagonista - visual inicial (moletom)", "Mia - moletom inicial",
             "Mia, survivor streamer, brown messy hair, oversized white hoodie with patches, "
             "leggings, sneakers, small bandage on cheek, phone in hand"),
    "dau_bep": ("Protagonista - chef (sala 4 cozinha)", "Mia - chef de abrigo",
                "Mia, blonde hair with blue tips, white chef jacket and toque, "
                "holding a ladle like a weapon, apron with duct tape"),
    "pijama": ("Protagonista - pijama (cena de história)", "Mia - pijama",
               "Mia, blonde hair with blue tips, polka-dot pajamas, sleepy, holding a plush zombie"),
    "Charlotte": ("Charlotte - rival rica (história)", "Charlotte - rival da elite",
                  "Charlotte, elegant rival survivor, long wavy blonde hair, round glasses, "
                  "tailored plaid uniform with tactical harness, confident arms crossed"),
    "Chef_Marco": ("Chef Marco - NPC cozinha", "Marco - cozinheiro do acampamento",
                   "Marco, tanned adult man chef, white chef uniform with apron, "
                   "friendly smile, holding a frying pan with dents"),
    "hana": ("Hana - empresária/agente", "Hana - coordenadora do abrigo",
             "Hana, adult woman, short brown bob, pink blazer over utility vest, "
             "clipboard and handheld radio, professional and kind"),
    "lara": ("Lara - funcionária", "Lara - médica de campo",
             "Lara, adult woman, long brown hair, navy medic uniform with red cross armband, "
             "first aid kit"),
    "sam_bikini": ("Sam - traje de praia", "Sam - mecânica na praia",
                   "Sam, adult woman, short orange hair, yellow wetsuit-style jumpsuit fully covered, "
                   "wrench on shoulder, playful"),
    "sam": ("Sam - amiga ruiva", "Sam - mecânica e saqueadora",
            "Sam, adult woman, short orange hair, school-style blazer with rolled sleeves, "
            "goggles on head, wrench and spray paint"),
    "rose": ("Rose - fotógrafa", "Rose - batedora com câmera",
             "Rose, adult woman, wavy pink hair, light cardigan and cargo pants, "
             "camera with zoom lens and binoculars, scout vibe"),
    "siriee": ("Siriee - celular assistente", "Siri-Z - celular companheiro",
               "cute smartphone mascot with cat-ear case, cracked screen showing kawaii face, "
               "tiny zombie keychain charm"),
    "teacher": ("Professor (sala 2)", "Prof. Silva - cientista da cura",
                "middle-aged man professor, glasses, beige lab coat over sweater, "
                "holding a notebook with formulas and a test tube"),
    "otaku": ("Fãs otaku", "Fãs zumbis fofos",
              "two cute cartoon zombie fans, green skin, stitched clothes, holding glow sticks "
              "and a fan banner, enthusiastic"),
    "Customer": ("Cliente (sala 5 loja)", "Sobrevivente compradora",
                 "adult woman survivor shopper, cardigan, holding a bag of canned food"),
    "Idol": ("Idol lendária", "A Streamer Lendária",
             "legendary idol streamer sitting cross-legged in a glowing aura, white stage outfit "
             "with protective jacket, mysterious and inspiring"),
}

# Roupas: índice da tabela Clothes -> (nome original, nome novo, descrição do traje)
OUTFITS = [
    ("Casual Outfit", "Moletom de Sobrevivente", "oversized white hoodie with patches, leggings, sneakers"),
    ("Maid Cosplay", "Avental de Barricada", "black and white housekeeper dress with long skirt, tool belt, hammer"),
    ("Playful girl", "Jaqueta de Patinadora", "pink bomber jacket, cargo shorts over tights, skateboard"),
    ("V-Tuber", "Streamer Neon", "neon techwear jacket, LED headset, holographic visor"),
    ("Sailor Uniform", "Uniforme da Resistência", "sailor-style uniform with long skirt, armband, backpack"),
    ("Student Uniform", "Exploradora Universitária", "college blazer, long skirt, map case and flashlight"),
    ("Nurse", "Médica de Campo", "long-sleeve medic uniform, red cross armband, medkit"),
    ("Cheerleader", "Torcida com Taco", "cheer uniform jacket and long pleated skirt, baseball bat with pom-pom"),
    ("Bunny Girl", "Macacão de Mecânica", "black mechanic jumpsuit with bunny-ear headband, wrench"),
    ("Active Streamer", "Streamer Tática", "tactical vest over hoodie, body camera, knee pads"),
    ("Sporty Girl", "Corredora Parkour", "tracksuit, running shoes, fingerless gloves"),
    ("Racer", "Motoqueira de Resgate", "padded motorcycle suit, helmet under arm"),
    ("Sensational summer", "Verão Pós-Apocalipse", "short-sleeve shirt, board shorts, sun hat, water canteen"),
    ("Lil'Devil", "Caçadora Noturna", "dark hooded coat, devil-horn headband, lantern"),
    ("Angel", "Anjo da Guarda", "white long coat, costume wings, healing kit"),
    ("Young lady", "Dama do Bunker", "elegant long dress with utility belt, umbrella as weapon"),
    ("Mystery", "Agente Misteriosa", "long trench coat, fedora, dossier folder"),
    ("Office uniforms", "Cientista da Cura", "lab coat over office suit, glowing antidote vial"),
    ("Sweet heart", "Armadura Coração", "pink padded armor with heart motifs, heart shield"),
    ("Rising Star", "Estrela em Ascensão", "sparkly idol jacket and long skirt, star microphone"),
    ("Elegant", "Gala do Fim do Mundo", "elegant long evening gown with leather jacket"),
    ("Sleepy Head", "Kigurumi de Zumbi", "cozy zombie onesie with hood, slippers"),
    ("Hot Star", "Rockstar Incendiária", "rock jacket with flame prints, electric guitar"),
    ("Trick or Treat", "Halloween Caça-Zumbis", "witch-hunter costume, pumpkin lantern, long cape"),
]

# Ícones skinN / avt_mainN (mapeados pelo visual; a ordem NÃO segue Clothes.txt)
# N -> (visual original, nome novo, descrição do traje novo)
SKINS = {
    "1": ("moletom branco", "Moletom de Sobrevivente", "oversized white hoodie with patches, leggings"),
    "2": ("uniforme marinheiro azul", "Uniforme da Resistência", "sailor-style uniform with long navy skirt, armband"),
    "3": ("top amarelo e short branco", "Verão de Vigia", "yellow short-sleeve shirt, white cargo shorts over leggings, sun visor"),
    "4": ("regata preta e short cáqui", "Exploradora Urbana", "black t-shirt, khaki cargo pants, utility belt"),
    "5": ("camisa e saia xadrez rosa", "Exploradora Universitária", "white shirt with pink bow, long pink plaid skirt, flashlight"),
    "6": ("maid", "Avental de Barricada", "black and white housekeeper dress with long skirt, tool belt, hammer"),
    "7": ("esportivo com boné", "Corredora Parkour", "sports jacket, track pants, cap, fingerless gloves"),
    "8": ("enfermeira", "Médica de Campo", "long-sleeve medic uniform, red cross armband, stethoscope"),
    "9": ("vestido rosa com babados", "Streamer Kawaii", "pink frilly long dress with protective padded jacket, LED headset"),
    "10": ("líder de torcida", "Torcida com Taco", "cheer jacket and long pleated skirt, baseball bat with pom-pom"),
    "11": ("vestido preto brilhante com orelhas", "Macacão de Mecânica", "black mechanic jumpsuit with bunny-ear headband, wrench"),
    "12": ("maid escura com asas de morcego", "Caçadora Noturna", "dark hooded coat with bat-wing cape, lantern"),
    "12New": ("jaqueta de corrida rosa/preta", "Motoqueira de Resgate", "padded pink and black racing suit, helmet under arm"),
    "13": ("suéter rosa", "Suéter Aconchegante", "long pink knitted sweater, jeans, mug of cocoa"),
    "14": ("vestido branco com asas", "Anjo da Guarda", "white long coat, costume wings, healing kit"),
    "15": ("top preto e saia vermelha xadrez", "Rockstar Incendiária", "black band t-shirt, long red plaid skirt, electric guitar"),
    "16": ("traje de verão amarelo", "Verão Pós-Apocalipse", "short-sleeve shirt, board shorts, sun hat, water canteen"),
    "17": ("gola alta preta", "Agente Misteriosa", "black turtleneck, long trench coat, dossier folder"),
    "18": ("uniforme de escritório", "Cientista da Cura", "lab coat over office suit, glowing antidote vial"),
    "19": ("marinheiro rosa", "Armadura Coração", "pink padded armor with heart motifs over sailor top, heart shield"),
    "20": ("vestido creme e preto", "Gala do Fim do Mundo", "elegant long evening gown with leather jacket"),
    "21": ("jaqueta azul clara", "Dama do Bunker", "light blue long dress with utility belt, umbrella as weapon"),
    "22": ("kigurumi amarelo", "Kigurumi de Zumbi", "cozy green zombie onesie with hood, slippers"),
    "23": ("idol marinheiro azul", "Estrela em Ascensão", "sparkly navy idol jacket and long skirt, star microphone"),
    "24": ("bruxa de halloween", "Halloween Caça-Zumbis", "witch-hunter costume, pumpkin lantern, long cape"),
}

# Salas: índice -> (nome original, nome novo, cenário)
ROOMS = [
    ("Bedroom (Room1)", "Quarto-Bunker", "small teen-style bedroom turned into a fortified bunker, boarded window"),
    ("Classroom (Room2)", "Escola Abandonada", "abandoned school classroom with barricaded doors, chalkboard notes"),
    ("Beach (Room3)", "Píer Fortificado", "beach pier with sandbag barricades, lifeguard tower, sunset sea"),
    ("Kitchen (Room4)", "Cozinha do Abrigo", "restaurant kitchen used as survivor canteen, pots and supplies"),
    ("Shop (Room5)", "Shopping Saqueado", "clothing boutique in a looted mall, mannequins and racks"),
    ("Bedroom (Room6)", "Cobertura Final", "luxury penthouse bedroom with helipad view, city skyline full of zombies far away"),
]

# Skills: nome original -> (nome novo, ícone/descrição)
SKILLS = {
    "talk": ("Conversa", "Dicas de Sobrevivência", "speech bubble with a small survival knife icon"),
    "joke": ("Piada", "Humor Sombrio", "laughing face with zombie hand"),
    "dance": ("Dança", "Dança Anti-Zumbi", "dancing silhouette dodging a zombie hand"),
    "sing": ("Canto", "Canção de Ninar p/ Zumbis", "microphone with musical notes and sleepy zombie"),
    "hype": ("Hype", "Grito de Guerra", "megaphone with shockwave"),
}

# room_01: prefixo do móvel -> (item original, item novo, descrição base)
ROOM1_ITEMS = {
    "sofa": ("Sofa", "Sofá Barricada", "sofa reinforced with planks and pillows"),
    "ghe": ("Sofa/Chair", "Poltrona Blindada", "armchair with metal plates"),
    "ban_live": ("Livestream Desk", "Mesa de Live", "streaming desk with monitors, ring light, ammo crate"),
    "ban": ("Table", "Mesa de Suprimentos", "table covered with canned food and radio"),
    "Ban_mt": ("Livestream Desk", "Mesa de Live", "streaming desk with computer"),
    "ke_sach": ("Bookshelf", "Estante de Suprimentos", "wall shelf with books, plants, first aid kit"),
    "ke_sah": ("Bookshelf", "Vitrine de Troféus", "display shelf with survival trophies"),
    "Gau": ("Teddy Bear", "Pelúcia Zumbi", "giant cute zombie plush with stitches"),
    "PC": ("Monitor", "Monitor de Câmeras", "computer monitors showing security cameras"),
    "case": ("Case", "Gabinete Gamer", "gaming PC tower with RGB fans and radiation sticker"),
    "tham": ("Carpet", "Tapete", "floor rug, isometric, with tape repairs"),
    "den_tran": ("Light", "Luminária", "ceiling lamp made from scavenged parts"),
    "goi_tua": ("Pillow", "Almofada", "throw pillow with zombie pattern"),
    "room": ("Room", "Quarto-Bunker (fundo)", "empty room background, boarded windows, wooden floor"),
    "Layer": ("Room layer", "Camada do quarto", "room overlay layer"),
}
TIERS = {
    "1": "tier 1: scavenged junk, worn and patched",
    "2": "tier 2: repaired and cleaned, some reinforcement",
    "3": "tier 3: reinforced with metal and neon accents",
    "4": "tier 4: high-tech fortified, glowing neon, premium",
}

# Sprites específicos da UI/ícones: nome -> (função original, prompt do novo asset)
UI_EXPLICIT = {
    "ButtonNoAds": ("Botão Remover Anúncios", "round button, crossed-out 'ADS' sign, zombie hand pushing it away"),
    "ButtonReduce": ("Botão Desconto 15%", "round button with outfit on mannequin and 'Sale 15%' ribbon"),
    "ButtonSmallMoney": ("Botão pacote pequeno de moedas", "round button with gift box full of coins"),
    "ButtonBigMoney": ("Botão pacote grande de moedas", "round button with stack of brain-shaped gold coins"),
    "Buttonx10Income": ("Booster x10 renda (ads)", "round button, flaming coins, label 'x10 Income'"),
    "ButtonX4Pass": ("Booster x4 passivo (ads)", "round button, stopwatch with coins, label 'x4 Passive'"),
    "Buttonx4Tap": ("Booster x4 toque (ads)", "round button, heart with tapping hand, label 'x4 Tap'"),
    "Button_VS": ("Botão batalha PK", "round button with 'VS' burst and zombie hands"),
    "Button_exit": ("Botão fechar", "round close X button"),
    "ButtonRankUp": ("Botão subir rank", "speech-bubble shaped green button"),
    "box_gift_3": ("Presente grátis", "gift box with heart, tied with caution tape"),
    "Icon_ADS": ("Ícone de vídeo recompensado", "clapperboard with play button and small zombie sticker"),
    "IconNoAds": ("Ícone sem anúncios", "crossed-out ADS badge"),
    "IconNoAd": ("Ícone sem anúncios", "crossed-out ADS badge"),
    "icon_ticket": ("Ticket pular anúncio", "purple ticket with play symbol"),
    "icon_ticket_replace": ("Ticket pular anúncio (alt)", "purple ticket with play symbol and sparkle"),
    "ticket_1_4": ("Ticket pular anúncio", "purple ticket with play symbol"),
    "Icon_Coin": ("Moeda", "gold coin with brain emblem"),
    "coin1_1": ("Moeda grande", "gold coin with brain emblem"),
    "coin2": ("Pilha de moedas", "stack of gold coins with brain emblem"),
    "coin_gift_1": ("Recompensa em moedas", "pile of gold coins"),
    "coins_afk": ("Recompensa AFK", "purple sack of coins with zzz"),
    "Icon_Follower": ("Seguidores", "group silhouette icon with survivor bandana"),
    "Icon_Follower2": ("Seguidores", "group silhouette icon"),
    "Icon_View": ("Visualizações", "eye icon with zombie stitches"),
    "eye_view": ("Visualizações", "eye icon"),
    "IconDonate": ("Doação", "hand giving heart-coin"),
    "Icon_claim_donate": ("Coletar doação", "envelope full of coins"),
    "IconAutoClick": ("Autoclique", "tapping hand with 'Auto'"),
    "IconX2Coin": ("Moeda x2", "coin with 'x2'"),
    "Iconx2Heart": ("Coração x2", "heart with 'x2'"),
    "x10Income": ("Booster x10 renda", "flaming coins"),
    "x4Click": ("Booster x4 toque", "heart with tapping hand"),
    "x4Pass": ("Booster x4 passivo", "stopwatch with coins"),
    "Logo_2x": ("Logo do jogo", f"game logo text '{GAME_TITLE}', bubbly letters, zombie hand and live badge"),
    "Artboard_1": ("Logo do jogo", f"game logo text '{GAME_TITLE}', bubbly letters, zombie hand and live badge"),
    "ThankYou": ("Agradecimento pós-compra", "cute thank-you sticker with Mia waving"),
    "newRoom": ("Nova sala", "door with 'NEW' badge and barricade"),
    "Light": ("Raios de luz", "radial light rays"),
    "Alert": ("Alerta/badge de notificação", "small red notification badge with exclamation mark"),
    "Arrow": ("Seta", "chunky arrow pointing right"),
    "ArrowLanguage": ("Seta do seletor de idioma", "small dropdown chevron"),
    "Checkmark": ("Marca de seleção", "bold check mark"),
    "CoinSaleOff": ("Moeda em promoção", "gold coin with discount tag"),
    "StarOff": ("Estrela vazia (avaliação)", "empty grey star"),
    "StarOn": ("Estrela cheia (avaliação)", "filled golden star"),
    "loading_2x": ("Ícone de carregamento", "circular loading spinner made of tiny zombie hands"),
    "seagull": ("Gaivota (sala praia)", "cartoon seagull flying"),
    "lightt_1": ("Brilho de destaque", "soft white glow flare"),
    "image_1_1": ("Imagem promocional", "promo sticker with Mia and coins"),
    "Layer_88": ("Camada da sala 1", "room overlay layer (window light)"),
    "Lara": ("Lara - avatar", "bust portrait of Lara, adult medic with long brown hair"),
}

# Emojis do chat (sprites base 0..36): mantêm função, ganham toque zumbi
EMOJI_HINTS = {
    0: "heart-eyes face", 1: "sparkles", 2: "eye-roll face", 3: "tears of joy face",
    4: "cute skull", 5: "popcorn", 6: "googly eyes", 7: "expressionless face",
    8: "anger symbol", 9: "red heart", 10: "nauseated face", 11: "fire", 12: "nail polish",
    13: "crown", 14: "explosion", 15: "pleading face", 16: "crying face", 17: "100 points",
    18: "two hearts", 19: "mind blown face", 20: "smiling face with hearts",
    21: "pointing right hand", 22: "pointing left hand", 23: "clapping hands",
    24: "raised hands", 25: "single eye", 26: "lips", 27: "money with wings", 28: "gift",
    29: "diamond", 30: "gold medal", 31: "trophy", 32: "megaphone", 33: "rose",
    34: "tulip bouquet", 35: "star-struck face", 36: "water drop",
}

EMOJI_NAMED = {
    "rose": "red rose", "eyes": "googly eyes", "fire": "fire", "fire_1": "fire",
    "heart-suit": "pink heart", "hundred-points": "100 points",
    "face-with-tears-of-joy": "tears of joy face", "money-with-wings": "money with wings",
    "smiling-face-with-heart-eyes": "heart-eyes face", "smiling-face-with-hearts": "smiling face with hearts",
    "star-struck": "star-struck face", "f47c089b-6f19-42d0-8fd7-b52629521905-Photoroom_2": "pink heart",
}

# Prompt de transformação (modelos de edição de imagem: entra o sprite original + esta instrução).
# O que MANTER do original em cada categoria; o resto deve ser redesenhado.
TRANSFORM_KEEP = {
    "personagem": "the same pose, body proportions, camera framing and expression intensity",
    "pose_corpo_inteiro": "the exact same pose and body framing (it is one frame of an outfit pose set)",
    "avatar": "the same bust framing, head angle and square crop",
    "roupa_icone": "the same garment layout on an invisible mannequin and the same framing",
    "movel_sala1": "the same isometric angle, footprint, size and shadow direction (it must fit the same spot in the room)",
    "cenario": "the same camera perspective, horizon line and floor area (props are placed on top of it)",
    "skill": "the same icon framing and level badge position",
    "emoji_chat": "the same emotion and readability at small size",
    "ui_monetizacao": "the same shape, button area and icon placement",
    "ui_botao": "the same shape and corner radius so it still works as a 9-slice button",
    "ui_moldura": "the same outline shape, border thickness and inner area so it still works as a 9-slice panel",
    "ui_icone": "the same silhouette size and meaning",
    "vfx": "the same shape, softness and alpha falloff",
}

TRANSFORM_TEMPLATE = (
    "Transform this image into a NEW original asset for the mobile game '{title}'. "
    "Keep {keep}, canvas {w}x{h}px and transparent background. "
    "Redesign everything else (character identity, face, hair, clothing, colors, details, line style) - "
    "do not just recolor or trace. Target: {target}. "
    "Style: {style}. Avoid: {negative}."
)

# Sprites de SDK/terceiros ou utilitários da engine: não recriar
IGNORE_PREFIXES = (
    "AppleInAppPurchase", "ApplePay", "GooglePay", "Coda", "GooTest", "Stripe",
    "Webshop", "stompy", "UICheckMark", "UIElement", "UIFoldout", "UIMask", "UISprite",
    "NumberButton", "PinDot", "RoundedCorner", "White1px", "InputFieldBackground",
    "Handle", "sem_nome", "GridBox", "AreaTex", "SearchTex", "Default-Particle",
    "LiberationSans", "NotoSans", "BalooDa2", "LDR_LLL", "EmojiOne",
    "error_icon", "info_icon", "warning_icon", "uninstall_icon",
)
IGNORE_SUFFIXES = ("-32", "-50", "-64", "-50_2x")
