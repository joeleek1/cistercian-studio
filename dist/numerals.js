(function (root) {
  'use strict';
  const MAX_DIGITS = 32;
  const segments = [[], [[0,0,1,0]], [[0,1,1,1]], [[0,0,1,1]], [[0,1,1,0]], [[0,0,1,0],[0,1,1,0]], [[1,0,1,1]], [[0,0,1,0],[1,0,1,1]], [[0,1,1,1],[1,0,1,1]], [[0,0,1,0],[0,1,1,1],[1,0,1,1]]];
  const colours = ['#b7653e','#377b83','#6c719a','#8d7243'];
  const names = ['Units', 'Tens', 'Hundreds', 'Thousands'];
  function parseInteger(raw) {
    const value = String(raw).trim();
    if (!value) throw new Error('Enter a whole number to begin.');
    if (!/^[+-]?(?:\d+|\d{1,3}(?:,\d{3})+)$/.test(value)) throw new Error('Use a whole number, such as 4723 or −125. Decimals and expressions are not supported here.');
    const clean = value.replace(/,/g, '');
    if (clean.replace(/^[+-]/, '').replace(/^0+(?=\d)/, '').length > MAX_DIGITS) throw new Error('Please use up to 32 digits per number.');
    return BigInt(clean);
  }
  function formatInteger(value) { return String(value).replace(/\B(?=(\d{3})+(?!\d))/g, ','); }
  function groups(value) {
    const negative = value < 0n;
    const digits = (negative ? -value : value).toString();
    const chunks = [];
    for (let end = digits.length; end > 0; end -= 4) chunks.unshift(digits.slice(Math.max(0, end - 4), end));
    return { negative, chunks: chunks.map((text, index) => ({value:Number(text), text, power:4*(chunks.length-index-1)})) };
  }
  function glyph(value, coloured = false, extraClass = '') {
    if (!Number.isInteger(value) || value < 0 || value > 9999) throw new Error('A single symbol holds 0–9999.');
    let lines = '<path d="M50 10V130" stroke="currentColor"/>';
    for (let place = 0; place < 4; place++) {
      const digit = Math.floor(value / (10 ** place)) % 10;
      const flipX = place % 2 ? -1 : 1;
      const bottom = place >= 2;
      const point = (x,y) => [50 + flipX*x*38, bottom ? 130-y*38 : 10+y*38];
      const path = segments[digit].map(([x1,y1,x2,y2]) => {
        const a = point(x1,y1), b = point(x2,y2);
        return `M${a[0]} ${a[1]}L${b[0]} ${b[1]}`;
      }).join('');
      if (path) lines += `<path data-place="${place}" d="${path}" stroke="${coloured ? colours[place] : 'currentColor'}"/>`;
    }
    return `<svg class="glyph ${extraClass}" xmlns="http://www.w3.org/2000/svg" viewBox="0 0 100 140" role="img" aria-label="Cistercian ${value}${value === 0 ? ', empty stem used as zero' : ''}"><g fill="none" stroke-width="4" stroke-linecap="round" stroke-linejoin="round">${lines}</g></svg>`;
  }
  function numberMarkup(value, coloured = false, labels = true) {
    const data = groups(value);
    return `<div class="numeral-sequence">${data.negative ? '<span class="negative-sign" aria-label="negative">−</span>' : ''}${data.chunks.map(g => `<div class="glyph-group">${glyph(g.value,coloured)}${labels ? `<span class="group-digits">${g.text}</span><span class="group-power">${g.power ? `× 10<sup>${g.power}</sup>` : '× 1'}</span>` : ''}</div>`).join('<span class="group-divider" aria-hidden="true"></span>')}</div>`;
  }
  function gcd(a,b) { a=a<0n?-a:a; b=b<0n?-b:b; while(b) [a,b]=[b,a%b]; return a; }
  function calculate(a,op,b) {
    let numerator, denominator=1n;
    switch(op) {
      case '+': numerator=a+b; break;
      case '-': numerator=a-b; break;
      case '*': numerator=a*b; break;
      case '/': if(b===0n) throw new Error('Division by zero is undefined. Try a different second number.'); numerator=a; denominator=b; break;
      default: throw new Error('Choose an operation.');
    }
    if (denominator<0n) { numerator=-numerator; denominator=-denominator; }
    const divisor=gcd(numerator,denominator);
    return {numerator:numerator/divisor, denominator:denominator/divisor};
  }
  function decimalPreview({numerator,denominator}, precision=12) {
    const sign=numerator<0n?'-':'';
    const n=numerator<0n?-numerator:numerator;
    const whole=n/denominator;
    let remainder=n%denominator, decimal='';
    for(let i=0;i<precision && remainder;i++) { remainder*=10n; decimal+=(remainder/denominator).toString(); remainder%=denominator; }
    return {text:sign+formatInteger(whole)+(decimal?'.'+decimal:''), approximate:remainder!==0n};
  }
  function resultMarkup(result,coloured=false) {
    if(result.denominator===1n) return numberMarkup(result.numerator,coloured);
    return `<div class="fraction"><div>${numberMarkup(result.numerator,coloured)}</div><div class="fraction-line" aria-label="divided by"></div><div>${numberMarkup(result.denominator,coloured)}</div></div>`;
  }
  function svgDocument(value,coloured=false) {
    const data=groups(value), offset=data.negative?45:0, width=data.chunks.length*120+offset+20;
    const symbols=data.chunks.map((g,i)=>`<g transform="translate(${20+offset+i*120},20)">${glyph(g.value,coloured).replace(/<svg[^>]*>/,'').replace('</svg>','')}</g>`).join('');
    return `<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 ${width} 180" width="${width}" height="180" color="#253e3b" role="img" aria-label="Cistercian representation of ${value}"><title>${value} in Cistercian notation; base-10000 extension</title>${data.negative?'<path d="M15 90h28" stroke="#253e3b" stroke-width="4"/>':''}${symbols}</svg>`;
  }
  const api={MAX_DIGITS,segments,colours,names,parseInteger,formatInteger,groups,glyph,numberMarkup,calculate,decimalPreview,resultMarkup,svgDocument};
  if(typeof module!=='undefined' && module.exports) module.exports=api;
  else root.Cistercian=api;
})(typeof globalThis!=='undefined'?globalThis:this);
