(function(root){
  'use strict';
  const cache=new Map();
  function partsAt(date,timeZone='Europe/London'){
    const zone=timeZone==='device'?Intl.DateTimeFormat().resolvedOptions().timeZone:timeZone;
    if(!cache.has(zone))cache.set(zone,{
      parts:new Intl.DateTimeFormat('en-GB',{timeZone:zone,year:'numeric',month:'2-digit',day:'2-digit',hour:'2-digit',minute:'2-digit',second:'2-digit',hourCycle:'h23'}),
      label:new Intl.DateTimeFormat('en-GB',{timeZone:zone,weekday:'long',day:'numeric',month:'long',year:'numeric'})
    });
    const formatter=cache.get(zone), values=Object.fromEntries(formatter.parts.formatToParts(date).map(part=>[part.type,part.value]));
    return {hours:values.hour,minutes:values.minute,seconds:values.second,day:values.day,month:values.month,year:values.year,label:formatter.label.format(date),zone};
  }
  const api={partsAt};
  if(typeof module!=='undefined'&&module.exports)module.exports=api;else root.StudioClock=api;
})(typeof globalThis!=='undefined'?globalThis:this);
