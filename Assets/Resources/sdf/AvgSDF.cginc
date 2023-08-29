#ifndef HG_AVG_SDF_INCLUDED
#define HG_AVG_SDF_INCLUDED

#define AA 8

#include "UnityCG.cginc"

float sdDeathStar( in float3 p2, in float ra, float rb, in float d )
{
  float2 p = float2( p2.x, length(p2.yz) );

  float a = (ra*ra - rb*rb + d*d)/(2.0*d);
  float b = sqrt(max(ra*ra-a*a,0.0));
  if( p.x*b-p.y*a > d*max(b-p.y,0.0) )
  {
    return length(p-float2(a,b));
  }
  else
  {
    return max((length(p)-ra), -(length(p-float2(d,0))-rb));
  }
}

inline float map( in float3 pos )
{
  float ra = 0.5;
  float rb = 0.35+0.20*cos(_Time.y*1.1+4.0);
  float di = 0.50+0.15*cos(_Time.y*1.7);
  return sdDeathStar(pos, ra, rb, di );
}

float3 calcNormal( in float3 pos )
{
  float2 e = float2(1.0,-1.0)*0.5773;
  const float eps = 0.0005;
  return normalize( e.xyy*map( pos + e.xyy*eps ) + 
  e.yyx*map( pos + e.yyx*eps ) + 
  e.yxy*map( pos + e.yxy*eps ) + 
  e.xxx*map( pos + e.xxx*eps ) );
}

float calcSoftshadow( in float3 ro, in float3 rd, float tmin, float tmax, const float k )
{
  float res = 1.0;
  float t = tmin;
  for( int i=0; i<64; i++ )
  {
    float h = map( ro + rd*t );
    res = min( res, k*h/t );
    t += clamp( h, 0.003, 0.10 );
    if( res<0.002 || t>tmax ) break;
  }
  return clamp( res, 0.0, 1.0 );
}

inline float4 SDFMainImage(float2 fragCoord)
{
  float4 fragColor;
  fragCoord *= _ScreenParams.xy;
  float2 iResolution = _ScreenParams.xy;
  // camera movement	
  float an = 1.0*sin(0.38*_Time.y+3.0);
  float3 ro = float3( 1.0*cos(an), -0.1, 1.0*sin(an) );
  float3 ta = float3( 0.0, 0.0, 0.0 );
  // camera matrix
  float3 ww = normalize( ta - ro );
  float3 uu = normalize( cross(ww,float3(0.0,1.0,0.0) ) );
  float3 vv = normalize( cross(uu,ww));
  
  
  float3 tot = 0.0;
  
#if AA>1
  for( int m=0; m<AA; m++ )
  for( int n=0; n<AA; n++ )
  {
    // pixel coordinates
    float2 o = float2(float(m),float(n)) / float(AA) - 0.5;
    float2 p = (2.0*(fragCoord+o)-iResolution.xy)/iResolution.y;
#else    
    float2 p = (2.0*fragCoord-iResolution.xy)/iResolution.y;
#endif

    // create view ray
    float3 rd = normalize( p.x*uu + p.y*vv + 1.5*ww );

    // raymarch
    const float tmax = 5.0;
    float t = 0.0;
    for( int i=0; i<256; i++ )
    {
      float3 pos = ro + t*rd;
      float h = map(pos);
      if( h<0.0001 || t>tmax ) break;
      t += h;
    }
    
    
    // shading/lighting	
    float3 col = 0.0;
    if( t<tmax )
    {
      float3 pos = ro + t*rd;
      float3 nor = calcNormal(pos);
      float3 lig = 0.57703;
      float dif = clamp( dot(nor,lig), 0.0, 1.0 );
      if( dif>0.001 ) dif *= calcSoftshadow( pos+nor*0.001, lig, 0.001, 1.0, 32.0 );
      float amb = 0.5 + 0.5*dot(nor,float3(0.0,1.0,0.0));
      col = float3(0.2,0.3,0.4)*amb + float3(0.8,0.7,0.5)*dif;
    }

    // gamma        
    col = sqrt( col );
    tot += col;
#if AA>1
  }
  tot /= float(AA*AA);
#endif

  fragColor = float4( tot, 1.0 );
  return fragColor;
}

#endif // HG_AVG_SDF_INCLUDED