float2 R;
float4 ColorA:COLOR = {0.0, 0.0, 0.0, 1};
float4 ColorB:COLOR = {1.0, 1.0, 1.0, 1};
bool Grey;
texture tex0;
sampler s0=sampler_state{Texture=(tex0);MipFilter=POINT;MinFilter=POINT;MagFilter=LINEAR;AddressU=WRAP;AddressV=WRAP;};

float4 p0(float2 x:TEXCOORD0):color{
       x=x+tex2D(s0,x*1.5+.2)*.6;
    float4 c=float4(sin(x.yx*28)+x*x.yx*2,length(x*2),sin(x.x*12+x.y*28))+tex2D(s0,x+.2)*3+tex2D(s0,(x.yx-.45)*.8+.45)*2+tex2D(s0,(x-.5)*998.8+.5)+tex2D(s0,(x.yx-.5)*798.8+.5+.31);
    c=frac(c*sqrt(float4(4.5,5.54,7.5,9)*2243));
    if(Grey)c=c.r;
    c=lerp(ColorA,ColorB,c);
    return c;
}
technique Noiz{pass pp0{vertexshader=null;pixelshader=compile ps_2_0 p0();}}
