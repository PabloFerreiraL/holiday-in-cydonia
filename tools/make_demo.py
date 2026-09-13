"""Create an original illustrative GIF; no game assets or captured footage."""
from pathlib import Path
from PIL import Image,ImageDraw,ImageFont
import sys

ROOT=Path(__file__).resolve().parents[1]
W,H=800,420
font_dir=Path('C:/Windows/Fonts')
def font(size,bold=False):
    candidates=[font_dir/('segoeuib.ttf' if bold else 'segoeui.ttf'),Path('/usr/share/fonts/truetype/dejavu/DejaVuSans.ttf')]
    for f in candidates:
        if f.exists():return ImageFont.truetype(str(f),size)
    return ImageFont.load_default(size=size)
TITLE=font(27,True);HEAD=font(23,True);BODY=font(17);SMALL=font(13);BUTTON=font(15,True)
COLORS=['#54df91','#ff717f','#f4cf61','#67a8ff','#eea05d']
STAGES=[(0,3.0,'Hold green','One press. Repeated notes.',[0]),
        (3.0,5.3,'Release','Following notes miss.',[]),
        (5.3,7.6,'Match the color','Green cannot hit red.',[0]),
        (7.6,10.6,'Hold both buttons','Matching chords can score.',[0,1])]
EVENTS=[(.8,[0],True),(1.6,[0],True),(2.4,[0],True),
        (3.8,[0],False),(4.6,[0],False),
        (6.1,[1],False),(6.9,[1],False),
        (8.4,[0,1],True),(9.2,[0,1],True),(10.0,[0,1],True)]
fps=20;frames=[]
palette_colors=['#10151f','#192232','#28354a','#eaf1ff','#a5b5cc','#54df91','#ff717f','#f4cf61','#67a8ff','#eea05d','#35485c','#122c24','#45232d','#8094ac']
palette=Image.new('P',(1,1));rgb=[]
for color in palette_colors:rgb.extend(tuple(bytes.fromhex(color[1:])))
palette.putpalette(rgb+[0]*(768-len(rgb)))

for index in range(round(10.6*fps)):
    t=index/fps
    stage=next(s for s in STAGES if s[0]<=t<s[1]);start,end,heading,subtitle,held=stage
    stage_i=STAGES.index(stage)
    im=Image.new('RGB',(W,H),'#10151f');d=ImageDraw.Draw(im)
    d.text((26,18),'Controller Hold-to-Hit',font=TITLE,fill='#eaf1ff')
    d.text((27,55),'Keep the buttons held. Keep the rhythm.',font=BODY,fill='#a5b5cc')
    d.rounded_rectangle((26,94,408,361),radius=16,fill='#192232')
    d.rounded_rectangle((429,94,774,361),radius=16,fill='#192232')
    xs=[74,145,216,287,358];top=119;line=308
    for lane,x in enumerate(xs):
        d.line((x,top,x,344),fill='#28354a',width=2)
        d.ellipse((x-19,line-19,x+19,line+19),fill=COLORS[lane] if lane in held else '#192232',outline=COLORS[lane],width=3)
    d.line((46,line,388,line),fill='#8094ac',width=1)
    for at,lanes,success in EVENTS:
        if not start<=at<end:continue
        if at-1.0<=t<=at+.35:
            y=line+(t-at)*188
            if t<at:
                if len(lanes)>1:d.line((xs[lanes[0]],y,xs[lanes[-1]],y),fill='#a5b5cc',width=4)
                for lane in lanes:d.rounded_rectangle((xs[lane]-21,y-8,xs[lane]+21,y+8),radius=7,fill=COLORS[lane])
            elif success:
                radius=int(22+(t-at)*35)
                for lane in lanes:d.ellipse((xs[lane]-radius,line-radius,xs[lane]+radius,line+radius),outline=COLORS[lane],width=3)
            elif y<350:
                for lane in lanes:
                    x=xs[lane];d.line((x-9,y-9,x+9,y+9),fill='#ff717f',width=4);d.line((x+9,y-9,x-9,y+9),fill='#ff717f',width=4)
    d.text((451,117),heading,font=HEAD,fill='#eaf1ff')
    d.text((451,154),subtitle,font=BODY,fill='#a5b5cc')
    d.text((451,202),'BUTTONS HELD',font=SMALL,fill='#a5b5cc')
    for lane,x in enumerate([475,534,593,652,711]):
        d.rounded_rectangle((x-21,231,x+21,272),radius=10,fill=COLORS[lane] if lane in held else '#28354a')
        d.text((x-6,240),'GRYBO'[lane],font=BUTTON,fill='#10151f' if lane in held else '#a5b5cc')
    recent=next(((at,ok) for at,lanes,ok in reversed(EVENTS) if start<=at<=t and t-at<.55),None)
    if recent:
        ok=recent[1]
        d.rounded_rectangle((451,301,752,340),radius=9,fill='#122c24' if ok else '#45232d')
        d.text((465,306),'MATCH  /  HIT' if ok else 'NO MATCH  /  MISS',font=BODY,fill='#54df91' if ok else '#ff717f')
    for i in range(4):
        x=27+i*193
        d.rounded_rectangle((x,376,x+176,380),radius=2,fill='#54df91' if i==stage_i else '#28354a')
    d.text((27,390),'Illustrated demo · not captured gameplay',font=SMALL,fill='#a5b5cc')
    d.text((583,390),'GH2 / PCSX2  +  GH3 / PC',font=SMALL,fill='#a5b5cc')
    frames.append(im.quantize(palette=palette,dither=Image.Dither.NONE))

out=ROOT/'assets/hold-to-hit-demo.gif'
out.parent.mkdir(parents=True,exist_ok=True)
frames[0].save(out,save_all=True,append_images=frames[1:],duration=1000//fps,loop=0,optimize=True,disposal=2)
# Optional contact sheet: python tools/make_demo.py --review
if '--review' in sys.argv:
    sheet=Image.new('RGB',(W*2,H*2))
    for n,t in enumerate([1.7,4.7,7.0,9.3]):sheet.paste(frames[round(t*fps)].convert('RGB'),((n%2)*W,(n//2)*H))
    review=ROOT/'analysis/hold-demo-review.png'
    review.parent.mkdir(parents=True,exist_ok=True)
    sheet.save(review)
print(f'Created {out}: {out.stat().st_size:,} bytes, {len(frames)} frames')
