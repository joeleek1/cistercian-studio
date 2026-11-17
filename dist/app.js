(() => {
  'use strict';
  const N=window.Cistercian, $=id=>document.getElementById(id);
  let coloured=false, currentValue=1993n, operation='+', lastResult=null, toastTimer;
  const tabNames=['convert','calculate','clock'];
  let lastClockKey='';
  function renderClock(force=false){
    if($('clock-panel').hidden&&!force)return;
    const now=new Date(), zone=$('clock-zone').value;
    const key=Math.floor(now.getTime()/1000)+'|'+zone+'|'+coloured;
    if(key===lastClockKey&&!force)return;
    lastClockKey=key;
    const parts=window.StudioClock.partsAt(now,zone);
    for(const name of ['hours','minutes','seconds','day','month','year']){
      const number=$('clock-'+name+'-number');
      if(number.textContent!==parts[name]||force){number.textContent=parts[name];$('clock-'+name+'-glyph').innerHTML=N.numberMarkup(BigInt(parts[name]),coloured,false);}
    }
    $('clock-date-label').textContent=parts.label;
    $('clock-zone-label').textContent='LIVE · '+parts.zone.replace(/_/g,' ').toUpperCase();
    $('live-time').setAttribute('aria-label',`Current time ${parts.hours}:${parts.minutes}:${parts.seconds} in ${parts.zone}`);
  }
  function notify(message){ $('toast').textContent=message; $('toast').hidden=false; clearTimeout(toastTimer); toastTimer=setTimeout(()=>$('toast').hidden=true,2500); }
  function renderConverter(){
    try {
      currentValue=N.parseInteger($('number-input').value.replace(/−/g,'-'));
      $('number-error').hidden=true; $('number-input').setAttribute('aria-invalid','false'); $('download-svg').disabled=false;
      $('convert-output').innerHTML=N.numberMarkup(currentValue,coloured);
      $('converted-value').textContent='= '+N.formatInteger(currentValue);
      const chunks=N.groups(currentValue).chunks;
      $('notation-tag').textContent=chunks.length===1?'ONE SYMBOL':chunks.length+' SYMBOLS · EXTENDED';
      if(chunks.length===1){
        $('breakdown-hint').textContent='Together, they make '+N.formatInteger(currentValue<0n?-currentValue:currentValue)+'.'+(currentValue<0n?' The minus sign is kept.':'');
        const v=chunks[0].value;
        $('breakdown').innerHTML=[3,2,1,0].map(place=>{
          const value=Math.floor(v/10**place)%10*10**place;
          return `<div class="place-card">${N.glyph(value,true)}<div><div class="place-name" style="color:${N.colours[place]}">${N.names[place]}</div><div class="place-value">${N.formatInteger(value)}</div><div class="place-location">${['Top right','Top left','Bottom right','Bottom left'][place]}</div></div></div>`;
        }).join('');
      } else {
        $('breakdown-hint').textContent='One symbol per four-digit group.';
        $('breakdown').innerHTML='<p class="breakdown-message">'+chunks.map(g=>`${N.formatInteger(g.value)} × ${N.formatInteger(10n**BigInt(g.power))}`).join(' + ')+' = '+N.formatInteger(currentValue<0n?-currentValue:currentValue)+(currentValue<0n?'. Keep the minus sign in front.':'.')+'<br>Read the symbols from left to right. An empty stem preserves a group of zeros.</p>';
      }
    } catch(error){
      currentValue=null; $('number-error').textContent=error.message; $('number-error').hidden=false; $('number-input').setAttribute('aria-invalid','true'); $('download-svg').disabled=true;
      $('convert-output').innerHTML='<p class="muted">Your symbol will appear here.</p>'; $('converted-value').textContent='—'; $('notation-tag').textContent='WAITING FOR A NUMBER'; $('breakdown-hint').textContent=''; $('breakdown').innerHTML='';
    }
  }
  function renderCalculator(){
    let a,b;
    try {
      let invalid=null;
      for(const key of ['a','b']){
        try { const value=N.parseInteger($('operand-'+key).value.replace(/−/g,'-')); if(key==='a')a=value;else b=value; $('operand-'+key).setAttribute('aria-invalid','false'); $('operand-'+key+'-glyph').innerHTML=N.numberMarkup(value,coloured,false); }
        catch(error){ $('operand-'+key).setAttribute('aria-invalid','true'); $('operand-'+key+'-glyph').innerHTML=''; invalid=error; }
      }
      if(invalid)throw invalid;
      lastResult=N.calculate(a,operation,b); $('calc-error').hidden=true; $('calc-result').hidden=false;
      const symbol={'+':'+','-':'−','*':'×','/':'÷'}[operation];
      $('decimal-equation').textContent=`${N.formatInteger(a)} ${symbol} ${b<0n?'('+N.formatInteger(b)+')':N.formatInteger(b)} =`;
      $('decimal-answer').textContent=N.formatInteger(lastResult.numerator)+(lastResult.denominator===1n?'':' / '+N.formatInteger(lastResult.denominator));
      const preview=N.decimalPreview(lastResult);
      $('decimal-detail').textContent=lastResult.denominator===1n?'Exact whole-number result.':(preview.approximate?'≈ ':'= ')+preview.text+(preview.approximate?'… (decimal preview)':' (decimal)');
      $('result-glyph').innerHTML=N.resultMarkup(lastResult,coloured);
      $('use-result').hidden=lastResult.denominator!==1n || String(lastResult.numerator).replace('-','').length>N.MAX_DIGITS;
    }catch(error){lastResult=null; $('calc-error').textContent=error.message; $('calc-error').hidden=false; $('calc-result').hidden=true;}
  }
  function setTab(name,focus=false){
    tabNames.forEach(key=>{const active=key===name; $(key+'-tab').setAttribute('aria-selected',String(active));$(key+'-tab').tabIndex=active?0:-1;$(key+'-tab').classList.toggle('active',active);$(key+'-panel').hidden=!active;});
    if(name==='calculate')renderCalculator();
    if(name==='clock')renderClock(true);
    if(focus)$(name+'-tab').focus();
  }
  tabNames.forEach(key=>$(key+'-tab').addEventListener('click',()=>setTab(key)));
  document.querySelector('.tabs').addEventListener('keydown',event=>{if(['ArrowLeft','ArrowRight','Home','End'].includes(event.key)){event.preventDefault();const index=tabNames.findIndex(key=>$(key+'-tab').getAttribute('aria-selected')==='true');setTab(tabNames[event.key==='Home'?0:event.key==='End'?tabNames.length-1:(index+(event.key==='ArrowRight'?1:tabNames.length-1))%tabNames.length],true);}});
  $('number-input').addEventListener('input',renderConverter);
  document.querySelectorAll('[data-example]').forEach(button=>button.addEventListener('click',()=>{$('number-input').value=button.dataset.example;renderConverter();}));
  $('colour-mode').addEventListener('change',()=>{coloured=$('colour-mode').checked;renderConverter();renderCalculator();renderClock(true);});
  $('clock-zone').addEventListener('change',()=>renderClock(true));
  setInterval(()=>renderClock(),250);
  document.addEventListener('visibilitychange',()=>{if(!document.hidden)renderClock(true);});
  ['a','b'].forEach(key=>$('operand-'+key).addEventListener('input',renderCalculator));
  document.querySelectorAll('[data-operation]').forEach(button=>button.addEventListener('click',()=>{operation=button.dataset.operation;document.querySelectorAll('[data-operation]').forEach(item=>{item.classList.toggle('selected',item===button);item.setAttribute('aria-pressed',String(item===button));});renderCalculator();}));
  $('use-result').addEventListener('click',()=>{if(!lastResult || lastResult.denominator!==1n)return;$('number-input').value=String(lastResult.numerator);renderConverter();setTab('convert');$('number-input').focus();});
  $('download-svg').addEventListener('click',()=>{if(currentValue===null)return;const blob=new Blob([N.svgDocument(currentValue,coloured)],{type:'image/svg+xml'});const url=URL.createObjectURL(blob);const link=document.createElement('a');link.href=url;link.download=`cistercian-${currentValue}.svg`;link.click();setTimeout(()=>URL.revokeObjectURL(url),1000);notify('Cistercian SVG saved.');});
  $('reference-table').innerHTML='<thead><tr><th scope="col">Place</th>'+Array.from({length:9},(_,i)=>`<th scope="col">${i+1}</th>`).join('')+'</tr></thead><tbody>'+[0,1,2,3].map(place=>`<tr><th scope="row" style="color:${N.colours[place]}">${N.names[place]}</th>${Array.from({length:9},(_,i)=>{const value=(i+1)*10**place;return `<td>${N.glyph(value,true)}<span>${N.formatInteger(value)}</span></td>`;}).join('')}</tr>`).join('')+'</tbody>';
  renderConverter();renderCalculator();
})();
