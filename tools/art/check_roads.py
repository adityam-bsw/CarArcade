"""Greyscale / silhouette / texture checks for road strips. python3 tools/art/check_roads.py <dir with road_*_1024x192.png>; writes _sheet_color_vs_grey.png."""
import sys, os, math
sys.path.insert(0, os.path.join(os.path.dirname(os.path.abspath(__file__)), '..', 'assets')); import pixel
D=sys.argv[1]; names=["asphalt","dirt","grass","ice","mud","glass"]
def lin(c):
    c/=255; return c/12.92 if c<=0.04045 else ((c+0.055)/1.055)**2.4
def L(r,g,b): return 0.2126*lin(r)+0.7152*lin(g)+0.0722*lin(b)
def luma(r,g,b): return 0.299*r+0.587*g+0.114*b
data={}
for n in names:
    w,h,rows=pixel.read_png(os.path.join(D,"road_%s_1024x192.png"%n)); data[n]=rows
BG=(110,170,225)
sheet=[]; sw=1024; gap=8
def comp(rows, grey, bg):
    out=[]
    for row in rows:
        o=[]
        for r,g,b,a in row:
            k=a/255; R=r*k+bg[0]*(1-k); G=g*k+bg[1]*(1-k); B=b*k+bg[2]*(1-k)
            if grey: y=int(luma(R,G,B)); R=G=B=y
            o.append((int(R),int(G),int(B),255))
        out.append(o)
    return out
# sheet: 2 columns (colour | grey) x 6 rows, each strip at half size (every 2nd px)
rowsout=[]
for n in names:
    c=comp(data[n],False,BG); g=comp(data[n],True,BG)
    for y in range(0,192,2):
        rowsout.append([c[y][x] for x in range(0,1024,2)]+[(40,40,40,255)]*4+[g[y][x] for x in range(0,1024,2)])
    rowsout.append([(40,40,40,255)]*(1024+4))
pixel.write_png(os.path.join(D,"_sheet_color_vs_grey.png"),1028+0,len(rowsout),rowsout) if False else None
w2=len(rowsout[0]); pixel.write_png(os.path.join(D,"_sheet_color_vs_grey.png"),w2,len(rowsout),rowsout)
# metrics
print("%-8s %7s %7s %7s | %8s %8s | %9s %9s"%("surface","meanL","bodyStd","topY_sd","topRough","botRough","dL_vs_asph","tex_energy"))
stats={}
for n in names:
    rows=data[n]; lum=[];top=[];bot=[]
    for x in range(1024):
        ty=None;by=None
        for y in range(192):
            if rows[y][x][3]>128:
                if ty is None: ty=y
                by=y
        top.append(ty if ty is not None else 0); bot.append(by if by is not None else 0)
    # body mean L in the opaque region
    vals=[];grid=[]
    for y in range(20,90):
        for x in range(0,1024):
            r,g,b,a=rows[y][x]
            if a>200: vals.append(luma(r,g,b))
    m=sum(vals)/len(vals); sd=(sum((v-m)**2 for v in vals)/len(vals))**.5
    # texture energy: mean abs horizontal gradient of luma in body
    ge=0;cnt=0
    for y in range(40,90):
        for x in range(0,1023):
            a=rows[y][x]; b=rows[y][x+1]
            if a[3]>200 and b[3]>200: ge+=abs(luma(*a[:3])-luma(*b[:3])); cnt+=1
            a2=rows[y+1][x]
            if a[3]>200 and a2[3]>200: ge+=abs(luma(*a[:3])-luma(*a2[:3])); cnt+=1
    mt=sum(top)/1024; tsd=(sum((t-mt)**2 for t in top)/1024)**.5
    rough=sum(abs(top[i]-top[i-1]) for i in range(1024))/1024
    br=sum(abs(bot[i]-bot[i-1]) for i in range(1024))/1024
    stats[n]=(m,sd,tsd,rough,br,ge/cnt,vals)
for n in names:
    s=stats[n]; print("%-8s %7.1f %7.1f %7.2f | %8.2f %8.2f | %9.1f %9.2f"%(n,s[0],s[1],s[2],s[3],s[4],s[0]-stats['asphalt'][0],s[5]))
print("pairwise |meanL diff| (0-255) / min texture-or-silhouette distinguishing metric:")
for i,a in enumerate(names):
    for b in names[i+1:]:
        dl=abs(stats[a][0]-stats[b][0]); dr=abs(stats[a][3]-stats[b][3])+abs(stats[a][4]-stats[b][4]); dt=abs(stats[a][5]-stats[b][5])
        flag = "" if (dl>=18 or dr>=1.0 or dt>=1.5) else "  <-- weak"
        print("  %-8s %-8s dL=%5.1f  dSilhouette=%5.2f  dTexture=%5.2f%s"%(a,b,dl,dr,dt,flag))
# colour code: mean hue of body
import colorsys
for n in names:
    r=g=b=k=0
    for y in range(20,90):
        for x in range(0,1024,4):
            p=data[n][y][x]
            if p[3]>200: r+=p[0];g+=p[1];b+=p[2];k+=1
    h,s,v=colorsys.rgb_to_hsv(r/k/255,g/k/255,b/k/255); print("%-8s mean body RGB=(%d,%d,%d) hue=%3d sat=%.2f"%(n,r/k,g/k,b/k,h*360,s))
