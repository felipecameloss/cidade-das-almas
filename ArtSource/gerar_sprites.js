// Gera os sprites do jogo: escreve um SVG por personagem e renderiza em PNG (512x512) com o Chrome headless.
// Uso: node ArtSource/gerar_sprites.js   (depois rode "Cidade das Almas > Construir Cena" no Unity)
const fs = require('fs');
const path = require('path');
const { execFileSync } = require('child_process');

const pastaSvg = __dirname;
const pastaPng = path.join(__dirname, '..', 'Assets', 'Art', 'Sprites');
const chrome = process.env.CHROME || 'C:/Program Files/Google/Chrome/Application/chrome.exe';
const perfilChrome = path.join(require('os').tmpdir(), 'cidade-almas-chrome');

const brilho = (id, desvio) => `
  <filter id="${id}" x="-50%" y="-50%" width="200%" height="200%">
    <feGaussianBlur stdDeviation="${desvio}" result="b"/>
    <feMerge><feMergeNode in="b"/><feMergeNode in="SourceGraphic"/></feMerge>
  </filter>`;

const svg = (defs, corpo) => `<svg xmlns="http://www.w3.org/2000/svg" width="256" height="256" viewBox="0 0 512 512">
<defs>${defs}</defs>
${corpo}
</svg>`;

// Medalhão art déco: anel dourado com losangos, fundo na cor do time e raios de sol atrás da figura
function medalhao({ fundo1, fundo2, anel1, anel2 }, figura) {
  const losangos = Array.from({ length: 16 }, (_, i) =>
    `<path transform="rotate(${i * 22.5} 256 256)" d="M256 14 L265 29 L256 44 L247 29Z" fill="#1a1208"/>`).join('');
  const raios = Array.from({ length: 24 }, (_, i) =>
    `<path transform="rotate(${i * 15} 256 256)" d="M256 256 L248 40 L264 40Z" fill="#fff" opacity="0.05"/>`).join('');

  return svg(`
    <radialGradient id="fundo" cx="50%" cy="38%" r="65%">
      <stop offset="0" stop-color="${fundo1}"/><stop offset="1" stop-color="${fundo2}"/>
    </radialGradient>
    <linearGradient id="anel" x1="0" y1="0" x2="0.3" y2="1">
      <stop offset="0" stop-color="${anel1}"/><stop offset="0.5" stop-color="${anel2}"/><stop offset="1" stop-color="${anel1}"/>
    </linearGradient>
    <clipPath id="dentro"><circle cx="256" cy="256" r="200"/></clipPath>
    ${brilho('brilho', 7)}`, `
  <circle cx="256" cy="256" r="248" fill="url(#anel)" stroke="#1a1208" stroke-width="8"/>
  ${losangos}
  <circle cx="256" cy="256" r="207" fill="#1a1208"/>
  <circle cx="256" cy="256" r="200" fill="url(#fundo)"/>
  <g clip-path="url(#dentro)">${raios}${figura}</g>
  <circle cx="256" cy="256" r="196" fill="none" stroke="${anel1}" stroke-width="3" opacity="0.7"/>`);
}

const sprites = {
  // Herói: chapéu fedora, sobretudo, cachecol safira e olhos brilhando
  Jogador: medalhao({ fundo1: '#3b7fe0', fundo2: '#0a1734', anel1: '#f0d690', anel2: '#8f6a2a' }, `
    <g stroke="#7fb8ff" stroke-width="3" stroke-opacity="0.55">
      <path d="M64 512 L96 384 Q120 318 204 296 L256 338 L308 296 Q392 318 416 384 L448 512Z" fill="#15111a"/>
      <path d="M204 296 L256 410 L226 300Z M308 296 L256 410 L286 300Z" fill="#2c2536"/>
      <path d="M208 200 Q206 292 256 304 Q306 292 304 200Z" fill="#0c0a10"/>
    </g>
    <path d="M212 284 Q256 312 300 284 L304 306 Q256 338 208 306Z" fill="#3d8bff"/>
    <g filter="url(#brilho)">
      <ellipse cx="234" cy="238" rx="11" ry="5" fill="#bdf0ff"/>
      <ellipse cx="278" cy="238" rx="11" ry="5" fill="#bdf0ff"/>
    </g>
    <path d="M188 198 Q182 122 212 104 Q238 94 256 114 Q274 94 300 104 Q330 122 324 198Z"
          fill="#1d1823" stroke="#7fb8ff" stroke-width="3" stroke-opacity="0.55"/>
    <path d="M187 168 Q256 178 325 168 L325 194 Q256 204 187 194Z" fill="#3d8bff"/>
    <ellipse cx="256" cy="202" rx="134" ry="26" fill="#120e16"/>
    <ellipse cx="256" cy="197" rx="130" ry="20" fill="#251f2c"/>`),

  // Tropa: capacete de trincheira, máscara de gás com lentes âmbar e rifle
  Tropa: medalhao({ fundo1: '#e8862c', fundo2: '#3e1604', anel1: '#e9c27a', anel2: '#7a5420' }, `
    <path d="M76 512 L102 392 Q128 330 208 314 L304 314 Q384 330 410 392 L436 512Z"
          fill="#2e2619" stroke="#ffb35a" stroke-width="3" stroke-opacity="0.5"/>
    <path d="M150 336 L330 512 L296 512 L132 356Z" fill="#4c3b22"/>
    <path d="M110 476 L384 296 L398 316 L126 496Z" fill="#17120c"/>
    <rect x="352" y="300" width="22" height="40" transform="rotate(-33 363 320)" fill="#5a4326"/>
    <path d="M198 220 Q194 322 256 334 Q318 322 314 220Z" fill="#3b3428"/>
    <g filter="url(#brilho)">
      <circle cx="229" cy="252" r="17" fill="#ffc062"/>
      <circle cx="283" cy="252" r="17" fill="#ffc062"/>
    </g>
    <circle cx="229" cy="252" r="18" fill="none" stroke="#15100a" stroke-width="7"/>
    <circle cx="283" cy="252" r="18" fill="none" stroke="#15100a" stroke-width="7"/>
    <rect x="234" y="284" width="44" height="46" rx="12" fill="#1d1810"/>
    <path d="M242 298 H270 M242 308 H270 M242 318 H270" stroke="#5a4c34" stroke-width="4"/>
    <path d="M172 214 Q178 128 256 124 Q334 128 340 214Z" fill="#5d5739"/>
    <path d="M192 196 Q200 150 256 144" stroke="#9c9360" stroke-width="7" fill="none" opacity="0.6"/>
    <ellipse cx="256" cy="216" rx="122" ry="24" fill="#4a452d" stroke="#2a2618" stroke-width="5"/>`),

  // Guardião: estátua de pedra com elmo art déco, olhos de fogo e runa no peito
  Guardiao: medalhao({ fundo1: '#5a2c12', fundo2: '#0d0705', anel1: '#c8904a', anel2: '#4e3014' }, `
    <path d="M36 512 L58 362 L148 300 L364 300 L454 362 L476 512Z" fill="#5f5b57"/>
    <path d="M58 362 L148 300 L192 342 L108 404Z" fill="#7d7873"/>
    <path d="M454 362 L364 300 L320 342 L404 404Z" fill="#7d7873"/>
    <path d="M198 342 L314 342 L292 470 L220 470Z" fill="#4a4744"/>
    <g filter="url(#brilho)"><path d="M256 372 L278 414 L256 456 L234 414Z" fill="#ff8a2a"/></g>
    <path d="M174 302 L168 172 L210 110 L256 90 L302 110 L344 172 L338 302 L256 334Z" fill="#6d6964"/>
    <path d="M256 90 L256 334" stroke="#4f4c49" stroke-width="7"/>
    <path d="M210 110 L222 170 M302 110 L290 170" stroke="#55524e" stroke-width="5"/>
    <path d="M190 206 L322 206 L310 240 L202 240Z" fill="#110c09"/>
    <g filter="url(#brilho)">
      <path d="M206 214 L248 214 L246 230 L210 230Z" fill="#ffb04a"/>
      <path d="M306 214 L264 214 L266 230 L302 230Z" fill="#ffb04a"/>
    </g>
    <path d="M186 150 L208 182 L196 204 M326 262 L304 284 L316 304 M120 420 L150 450" stroke="#33312f" stroke-width="4" fill="none"/>`),

  // Alma: chama azul-ciano com núcleo branco e aura
  Alma: svg(`
    <radialGradient id="aura" cx="50%" cy="58%" r="50%">
      <stop offset="0" stop-color="#8fefff" stop-opacity="0.75"/>
      <stop offset="0.45" stop-color="#2fb6ff" stop-opacity="0.28"/>
      <stop offset="1" stop-color="#2fb6ff" stop-opacity="0"/>
    </radialGradient>
    <linearGradient id="chama" x1="0" y1="1" x2="0" y2="0">
      <stop offset="0" stop-color="#ffffff"/><stop offset="0.45" stop-color="#8ff0ff"/>
      <stop offset="1" stop-color="#2a9dff" stop-opacity="0.15"/>
    </linearGradient>
    ${brilho('suave', 10)}
    <filter id="borrao"><feGaussianBlur stdDeviation="14"/></filter>`, `
  <circle cx="256" cy="296" r="214" fill="url(#aura)"/>
  <path filter="url(#suave)" fill="url(#chama)" d="M256 56 C292 132 366 186 356 284 C348 356 304 398 256 398
        C208 398 164 356 156 284 C150 216 196 196 212 140 C226 188 240 200 252 188 C240 146 238 106 256 56Z"/>
  <path fill="#c9f8ff" opacity="0.8" d="M300 130 C312 170 332 196 326 240 C316 210 300 190 300 130Z"/>
  <circle cx="256" cy="304" r="70" fill="#ffffff" filter="url(#borrao)"/>
  <circle cx="256" cy="304" r="36" fill="#ffffff"/>
  <path d="M256 268 L264 296 L292 304 L264 312 L256 340 L248 312 L220 304 L248 296Z" fill="#e8fdff"/>`),

  // Poça de corrupção: mancha escura com borda verde e sigilo oculto
  Poca: svg(`
    <radialGradient id="poca" cx="50%" cy="50%" r="50%">
      <stop offset="0" stop-color="#050c04"/><stop offset="0.7" stop-color="#173f0f"/>
      <stop offset="1" stop-color="#3fae2a"/>
    </radialGradient>
    ${brilho('verde', 9)}`, `
  <g filter="url(#verde)">
    <path fill="url(#poca)" stroke="#7dff5a" stroke-width="6" d="M256 70 C330 64 384 108 420 160 C460 220 446 300 420 352
          C392 410 330 446 256 442 C186 440 118 412 88 350 C62 296 64 220 96 168 C130 112 186 74 256 70Z"/>
  </g>
  <g fill="none" stroke="#9dff7a" stroke-width="5" opacity="0.55">
    <circle cx="256" cy="256" r="118"/>
    <circle cx="256" cy="256" r="40"/>
    <path d="M256 138 L358 315 L154 315Z"/>
    ${Array.from({ length: 12 }, (_, i) =>
      `<path transform="rotate(${i * 30 + 15} 256 256)" d="M256 124 V108 M250 112 H262"/>`).join('')}
  </g>
  <circle cx="200" cy="170" r="10" fill="#9dff7a" opacity="0.7"/>
  <circle cx="340" cy="380" r="7" fill="#9dff7a" opacity="0.6"/>
  <circle cx="380" cy="200" r="5" fill="#9dff7a" opacity="0.6"/>`),
};

fs.mkdirSync(pastaPng, { recursive: true });
for (const [nome, conteudo] of Object.entries(sprites)) {
  const arquivoSvg = path.join(pastaSvg, nome + '.svg');
  const arquivoPng = path.join(pastaPng, nome + '.png');
  fs.writeFileSync(arquivoSvg, conteudo);
  execFileSync(chrome, [
    '--headless=new', '--disable-gpu', '--hide-scrollbars', '--no-first-run',
    `--user-data-dir=${perfilChrome}`,
    '--default-background-color=00000000', '--force-device-scale-factor=2', '--window-size=256,256',
    `--screenshot=${arquivoPng}`, 'file:///' + arquivoSvg.replace(/\\/g, '/'),
  ], { stdio: 'ignore' });
  console.log('ok', nome);
}
