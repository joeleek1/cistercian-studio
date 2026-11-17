const test=require('node:test');
const assert=require('node:assert/strict');
const N=require('../dist/numerals.js');

test('all nine units follow the supplied chart, including detached six',()=>{
  const paths=['M50 10L88 10','M50 48L88 48','M50 10L88 48','M50 48L88 10','M50 10L88 10M50 48L88 10','M88 10L88 48','M50 10L88 10M88 10L88 48','M50 48L88 48M88 10L88 48','M50 10L88 10M50 48L88 48M88 10L88 48'];
  paths.forEach((path,i)=>assert.ok(N.glyph(i+1).includes(`d="${path}"`),`digit ${i+1}`));
});

test('quadrant orientation and composed examples match the reference',()=>{
  const fixtures={
    1993:['M50 10L88 48','M50 10L12 10M50 48L12 48M12 10L12 48','M50 130L88 130M50 92L88 92M88 130L88 92','M50 130L12 130'],
    4723:['M50 10L88 48','M50 48L12 48','M50 130L88 130M88 130L88 92','M50 92L12 130'],
    6859:['M50 10L88 10M50 48L88 48M88 10L88 48','M50 10L12 10M50 48L12 10','M50 92L88 92M88 130L88 92','M12 130L12 92'],
    7085:['M50 10L88 10M50 48L88 10','M50 48L12 48M12 10L12 48',null,'M50 130L12 130M12 130L12 92'],
    9433:['M50 10L88 48','M50 10L12 48','M50 92L88 130','M50 130L12 130M50 92L12 92M12 130L12 92']
  };
  for(const [value,paths] of Object.entries(fixtures)){
    const svg=N.glyph(Number(value));
    paths.forEach((path,place)=>path?assert.ok(svg.includes(`data-place="${place}" d="${path}"`),`${value} place ${place}`):assert.ok(!svg.includes(`data-place="${place}"`)));
  }
});

test('all 10,000 symbols have distinct geometry',()=>{
  const shapes=new Set();
  for(let n=0;n<10000;n++)shapes.add(N.glyph(n).match(/<g[\s\S]*<\/g>/)[0]);
  assert.equal(shapes.size,10000);
  assert.equal((N.glyph(0).match(/<path/g)||[]).length,1);
});

test('extended notation preserves place values and internal zero groups',()=>{
  for(const value of [0n,9999n,10000n,100000001n,-12345678n,99999999999999999999999999999999n]){
    const {negative,chunks}=N.groups(value);
    const decoded=chunks.reduce((sum,g)=>sum+BigInt(g.value)*10n**BigInt(g.power),0n)*(negative?-1n:1n);
    assert.equal(decoded,value);
  }
  assert.deepEqual(N.groups(100000001n).chunks.map(g=>g.text),['1','0000','0001']);
  assert.equal(N.groups(10000n).chunks.length,2);
});

test('input validation and integer precision',()=>{
  assert.equal(N.parseInteger(' -12,345,678 '),-12345678n);
  assert.equal(N.parseInteger('+0012'),12n);
  assert.equal(N.parseInteger('9007199254740993'),9007199254740993n);
  assert.equal(N.parseInteger('0'.repeat(40)+'1'),1n);
  for(const value of ['', '1.5','1e4','1,23','12abc','--2','1 000','9'.repeat(33)])assert.throws(()=>N.parseInteger(value));
});

test('exact arithmetic supports all operators, signs, large numbers and zero',()=>{
  const cases=[[1993n,'+',30n,2023n,1n],[5n,'-',12n,-7n,1n],[9999n,'*',9999n,99980001n,1n],[7n,'/',2n,7n,2n],[2n,'/',6n,1n,3n],[3n,'/',-6n,-1n,2n],[-3n,'/',-6n,1n,2n],[0n,'/',9n,0n,1n],[9007199254740993n,'+',1n,9007199254740994n,1n]];
  for(const [a,op,b,numerator,denominator] of cases)assert.deepEqual(N.calculate(a,op,b),{numerator,denominator});
  assert.throws(()=>N.calculate(1n,'/',0n),/Division by zero/);
  assert.throws(()=>N.calculate(1n,'?',2n),/Choose an operation/);
  const max=N.parseInteger('9'.repeat(32));
  assert.equal(N.calculate(max,'*',max).numerator,max*max);
});

test('fraction rendering and decimal preview distinguish exact and approximate results',()=>{
  assert.deepEqual(N.decimalPreview(N.calculate(1n,'/',3n)),{text:'0.333333333333',approximate:true});
  assert.deepEqual(N.decimalPreview(N.calculate(-1n,'/',8n)),{text:'-0.125',approximate:false});
  assert.ok(N.resultMarkup(N.calculate(7n,'/',2n)).includes('fraction-line'));
  assert.ok(!N.resultMarkup(N.calculate(8n,'/',2n)).includes('fraction-line'));
});

test('SVG download is self contained and includes all grouped stems',()=>{
  const svg=N.svgDocument(-100000001n,true);
  assert.ok(svg.includes('xmlns="http://www.w3.org/2000/svg"'));
  assert.equal((svg.match(/M50 10V130/g)||[]).length,3);
  assert.ok(svg.includes('Cistercian representation of -100000001'));
  assert.ok(!svg.includes('undefined'));
});
