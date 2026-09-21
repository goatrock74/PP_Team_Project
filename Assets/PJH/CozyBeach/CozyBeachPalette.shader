Shader "PJH/CozyBeachPalette" {
 Properties {
 [PerRendererData] _MainTex ("Sprite",2D)="white" {}
 _Land("Land",Color)=(0.97,0.86,0.59,1)
 _Wet("Wet sand",Color)=(0.84,0.72,0.48,1)
 _Water("Water",Color)=(0.40,0.79,0.77,1)
 _BoundaryTex("Coast scanline data",2D)="black" {}
 _UseBoundary("Use continuous coast",Float)=0
 _BoundaryY("Coast bottom",Float)=0
 _BoundaryHeight("Coast height",Float)=64
 }
 SubShader {
 Tags {"Queue"="Transparent" "RenderType"="Transparent" "CanUseSpriteAtlas"="True"}
 Cull Off ZWrite Off
 Blend SrcAlpha OneMinusSrcAlpha
 Pass {
 CGPROGRAM
 #pragma vertex vert
 #pragma fragment frag
 #include "UnityCG.cginc"
 struct a {float4 v:POSITION;float2 uv:TEXCOORD0;fixed4 c:COLOR;};
 struct b {float4 v:SV_POSITION;float2 uv:TEXCOORD0;fixed4 c:COLOR;float2 world:TEXCOORD1;};
 sampler2D _MainTex;fixed4 _Land,_Wet,_Water;
 sampler2D _BoundaryTex;float _UseBoundary,_BoundaryY,_BoundaryHeight;
 b vert(a i){b o;o.v=UnityObjectToClipPos(i.v);o.uv=i.uv;o.c=i.c;o.world=mul(unity_ObjectToWorld,i.v).xy;return o;}
 float hash(float2 p){return frac(sin(dot(p,float2(127.1,311.7)))*43758.5453);}
 float patch(float2 p){
 float2 q=floor(p),f=frac(p);f=f*f*(3-2*f);
 return lerp(lerp(hash(q),hash(q+float2(1,0)),f.x),lerp(hash(q+float2(0,1)),hash(q+1),f.x),f.y);
 }
 fixed4 frag(b i):SV_Target{
 fixed4 c=tex2D(_MainTex,i.uv);
 bool sourceWater=c.b>c.r*1.08;
 bool oceanPalette=_Land.b>_Land.r;
 bool grassPalette=!oceanPalette&&_Land.g>_Land.r*1.15;
 float2 px=floor(i.world*16);
 float inland=0;
 bool boundaryApplied=_UseBoundary>0.5;
 float lookupY=((px.y+0.5)/16-_BoundaryY)/_BoundaryHeight;
 if(boundaryApplied&&grassPalette){
 float nearbyAbove=tex2D(_BoundaryTex,float2(0.5,lookupY+0.25/_BoundaryHeight)).r;
 float nearbyBelow=tex2D(_BoundaryTex,float2(0.5,lookupY-0.25/_BoundaryHeight)).r;
 // Horizontal entrance edges use their original painted masks.
 if(abs(nearbyAbove-nearbyBelow)>2)boundaryApplied=false;
 }
 if(boundaryApplied){
 float coast=tex2D(_BoundaryTex,float2(0.5,lookupY)).r;
 inland=coast*16-(px.x+0.5);
 sourceWater=inland<0;
 }
 fixed3 rgb=sourceWater?_Water.rgb:lerp(_Wet.rgb,_Land.rgb,saturate((c.r-0.62)*4.5));
 if(boundaryApplied&&!sourceWater){
 float edgeWidth=grassPalette?2.0:5.0;
 float edgeShade=grassPalette?0.65:0.40;
 rgb=lerp(_Land.rgb,_Wet.rgb,edgeShade*(1-step(edgeWidth,inland)));
 }
 if(boundaryApplied&&!grassPalette&&sourceWater&&inland>-1.2)
 rgb=lerp(_Water.rgb,_Land.rgb,0.48);
 // World-aligned, native 16-pixel details do not repeat at tile boundaries.
 bool isGrass=grassPalette&&!sourceWater;
 bool isSand=(!grassPalette&&!oceanPalette&&!sourceWater)||(grassPalette&&sourceWater);
 if(isGrass){
 float broad=patch(floor(px/3)/24)-0.5;
 rgb+=float3(0.052,0.058,0.024)*broad;
 float2 cell=floor(px/12),local=px-cell*12;
 float seed=hash(cell+7);
 float2 origin=2+floor(float2(hash(cell+21),hash(cell+73))*7);
 float2 d=local-origin;
 bool blades=(d.x==0&&d.y>=0&&d.y<=2)||(abs(d.x)==1&&d.y==1)||(abs(d.x)==2&&d.y==2);
 if(seed<0.32&&blades)rgb*=0.89;
 else if(seed<0.32&&d.x==1&&d.y==0)rgb+=float3(0.042,0.052,0.015);
 if(hash(px+float2(611,87))>0.988)rgb*=0.96;
 }else if(isSand){
 float broad=patch(floor(px/3)/31)-0.5;
 rgb+=float3(0.023,0.031,0.030)*broad;
 float grain=hash(px+float2(37,117)),pocket=patch(px/43);
 if(grain>0.978+0.012*step(0.52,pocket))rgb-=float3(0.040,0.052,0.042);
 else if(grain<0.012)rgb+=float3(0.016,0.017,0.017);
 float2 cell=floor(px/27),d=px-cell*27-floor(float2(hash(cell+81),hash(cell+49))*21)-3;
 if(hash(cell+110)<0.17&&((d.y==0&&abs(d.x)<=1)||(d.y==1&&d.x==2)))rgb-=float3(0.035,0.040,0.035);
 }else{
 rgb+=(patch(floor(px/4)/35)-0.5)*float3(0.012,0.020,0.021);
 }
 return fixed4(rgb,c.a)*i.c;
 }
 ENDCG
 }
 }
}
