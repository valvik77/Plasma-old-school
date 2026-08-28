#include <windows.h>
#include <d3d11.h>
#include <d3dcompiler.h>
#include <wrl/client.h>
#include <algorithm>
#include <cstring>

using Microsoft::WRL::ComPtr;

struct FrameData {
    float resolution[2], time, scale;
    float origin[2], warp, density;
    float pulse, rotation, brightness, contrast;
    float phaseA, phaseB, phaseC, colorShift;
    float color0[4], color1[4], color2[4], color3[4];
    float mirror[2], pixelBlock, scanlineSpacing;
    float scanlineOpacity, seed;
    int colorCycle, movingOrigin;
    int scanlines, vignette;
    float renderScale, padding;
};

static const char* ShaderSource = R"(
cbuffer Frame : register(b0) {
 float2 resolution; float time; float scale; float2 origin; float warp; float density;
 float pulse; float rotation; float brightness; float contrast; float phaseA; float phaseB; float phaseC; float colorShift;
 float4 color0; float4 color1; float4 color2; float4 color3; float2 mirror; float pixelBlock; float scanlineSpacing;
 float scanlineOpacity; float seed; int colorCycle; int movingOrigin; int scanlines; int vignette; float renderScale; float padding;
};
struct VSOut { float4 pos:SV_POSITION; float2 uv:TEXCOORD0; };
VSOut VS(uint id:SV_VertexID) { VSOut o; o.uv=float2((id<<1)&2,id&2); o.pos=float4(o.uv*float2(2,-2)+float2(-1,1),0,1); return o; }
float3 palette(float v) { float p=frac(v)*4; int i=(int)floor(p); float a=smoothstep(0,1,frac(p)); if(i==0)return lerp(color0.rgb,color1.rgb,a); if(i==1)return lerp(color1.rgb,color2.rgb,a); if(i==2)return lerp(color2.rgb,color3.rgb,a); return lerp(color3.rgb,color0.rgb,a); }
float4 PS(VSOut input):SV_TARGET {
 float2 sc=input.uv*resolution; if(mirror.x<0)sc.x=resolution.x-sc.x; if(mirror.y<0)sc.y=resolution.y-sc.y;
 float2 fc=floor(sc/max(1,pixelBlock))*max(1,pixelBlock)+pixelBlock*.5; float zoom=(160/resolution.x)*scale; float2 p=fc*zoom;
 float2 center=resolution*origin*zoom; if(movingOrigin!=0)center+=float2(sin(time*.5)*50*scale,cos(time/3)*30*scale);
 float d=max(.5,density); float v1=.5+.5*sin(p.x*d/16+phaseA*.08); float v2=.5+.5*sin((p.x*sin(time*.5*rotation+phaseB*.04)+p.y*cos(time/max(.5,3/max(.1,rotation))))/(8/d));
 float v3=.5+.5*sin(length(p-center)/(8/d)-time*.25); float v4=.5+.5*sin(time+length(p)/(8/d)); float b=(v1+v2+lerp(.5,v3,saturate(pulse/1.5))+v4)*.25;
 float extra=.5+.5*sin((p.x+p.y)/(11/d)+time*.7+phaseC); float value=lerp(b,(b+extra)*.5,saturate(warp*.35)); if(colorCycle!=0)value+=colorShift;
 float3 c=(palette(value)-.5)*contrast+.5; c*=brightness;
 if(scanlines!=0 && fmod(sc.y,scanlineSpacing)>scanlineSpacing-1)c*=1-scanlineOpacity;
 if(vignette!=0){float2 uv=input.uv;float edge=min(min(uv.x,1-uv.x),min(uv.y,1-uv.y));c*=lerp(.62,1,smoothstep(0,.36,edge));}
 return float4(saturate(c),1);
})";

static const char* BlitShaderSource = R"(
Texture2D sceneTexture : register(t0);
SamplerState pointSampler : register(s0);
cbuffer Frame : register(b0) {
 float2 resolution; float time; float scale; float2 origin; float warp; float density;
 float pulse; float rotation; float brightness; float contrast; float phaseA; float phaseB; float phaseC; float colorShift;
 float4 color0; float4 color1; float4 color2; float4 color3; float2 mirror; float pixelBlock; float scanlineSpacing;
 float scanlineOpacity; float seed; int colorCycle; int movingOrigin; int scanlines; int vignette; float renderScale; float padding;
};
struct VSOut { float4 pos:SV_POSITION; float2 uv:TEXCOORD0; };
float4 PS(VSOut input):SV_TARGET {
 float4 color=sceneTexture.Sample(pointSampler,input.uv);
 if(scanlines!=0 && fmod(input.pos.y,scanlineSpacing)>scanlineSpacing-1) color.rgb*=1-scanlineOpacity;
 return color;
})";

class Renderer {
public:
    HWND hwnd{}; UINT width{}, height{};
    ComPtr<ID3D11Device> device; ComPtr<ID3D11DeviceContext> context; ComPtr<IDXGISwapChain> swap;
    ComPtr<ID3D11RenderTargetView> target, sceneTarget; ComPtr<ID3D11ShaderResourceView> sceneView;
    ComPtr<ID3D11VertexShader> vs; ComPtr<ID3D11PixelShader> ps, blitPs; ComPtr<ID3D11Buffer> constants; ComPtr<ID3D11SamplerState> sampler;
    UINT sceneWidth{}, sceneHeight{};
    bool Init(HWND window) {
        hwnd=window; DXGI_SWAP_CHAIN_DESC desc{}; desc.BufferCount=2; desc.BufferDesc.Format=DXGI_FORMAT_B8G8R8A8_UNORM; desc.BufferUsage=DXGI_USAGE_RENDER_TARGET_OUTPUT; desc.OutputWindow=hwnd; desc.SampleDesc.Count=1; desc.Windowed=TRUE; desc.SwapEffect=DXGI_SWAP_EFFECT_DISCARD;
        D3D_FEATURE_LEVEL requested[]={D3D_FEATURE_LEVEL_11_0,D3D_FEATURE_LEVEL_10_0}; D3D_FEATURE_LEVEL obtained{};
        if(FAILED(D3D11CreateDeviceAndSwapChain(nullptr,D3D_DRIVER_TYPE_HARDWARE,nullptr,0,requested,2,D3D11_SDK_VERSION,&desc,&swap,&device,&obtained,&context))) return false;
        ComPtr<ID3DBlob> vblob,pblob,error; if(FAILED(D3DCompile(ShaderSource,strlen(ShaderSource),nullptr,nullptr,nullptr,"VS","vs_4_0",D3DCOMPILE_OPTIMIZATION_LEVEL3,0,&vblob,&error)))return false;
        if(FAILED(D3DCompile(ShaderSource,strlen(ShaderSource),nullptr,nullptr,nullptr,"PS","ps_4_0",D3DCOMPILE_OPTIMIZATION_LEVEL3,0,&pblob,&error)))return false;
        if(FAILED(device->CreateVertexShader(vblob->GetBufferPointer(),vblob->GetBufferSize(),nullptr,&vs)))return false;
        if(FAILED(device->CreatePixelShader(pblob->GetBufferPointer(),pblob->GetBufferSize(),nullptr,&ps)))return false;
        ComPtr<ID3DBlob> blitBlob; if(FAILED(D3DCompile(BlitShaderSource,strlen(BlitShaderSource),nullptr,nullptr,nullptr,"PS","ps_4_0",D3DCOMPILE_OPTIMIZATION_LEVEL3,0,&blitBlob,&error)))return false;
        if(FAILED(device->CreatePixelShader(blitBlob->GetBufferPointer(),blitBlob->GetBufferSize(),nullptr,&blitPs)))return false;
        D3D11_BUFFER_DESC bd{}; bd.ByteWidth=(sizeof(FrameData)+15)&~15; bd.Usage=D3D11_USAGE_DYNAMIC; bd.BindFlags=D3D11_BIND_CONSTANT_BUFFER; bd.CPUAccessFlags=D3D11_CPU_ACCESS_WRITE;
        if(FAILED(device->CreateBuffer(&bd,nullptr,&constants)))return false;
        D3D11_SAMPLER_DESC sd{}; sd.Filter=D3D11_FILTER_MIN_MAG_MIP_POINT; sd.AddressU=D3D11_TEXTURE_ADDRESS_CLAMP; sd.AddressV=D3D11_TEXTURE_ADDRESS_CLAMP; sd.AddressW=D3D11_TEXTURE_ADDRESS_CLAMP; sd.MaxLOD=D3D11_FLOAT32_MAX;
        return SUCCEEDED(device->CreateSamplerState(&sd,&sampler));
    }
    bool Resize(UINT w,UINT h){if(w==0||h==0)return false;if(target&&w==width&&h==height)return true;target.Reset();context->OMSetRenderTargets(0,nullptr,nullptr);if(FAILED(swap->ResizeBuffers(0,w,h,DXGI_FORMAT_UNKNOWN,0)))return false;ComPtr<ID3D11Texture2D>b;if(FAILED(swap->GetBuffer(0,IID_PPV_ARGS(&b))))return false;if(FAILED(device->CreateRenderTargetView(b.Get(),nullptr,&target)))return false;width=w;height=h;return true;}
    bool EnsureSceneTarget(UINT w,UINT h){if(sceneTarget&&w==sceneWidth&&h==sceneHeight)return true;sceneTarget.Reset();sceneView.Reset();D3D11_TEXTURE2D_DESC td{};td.Width=w;td.Height=h;td.MipLevels=1;td.ArraySize=1;td.Format=DXGI_FORMAT_B8G8R8A8_UNORM;td.SampleDesc.Count=1;td.Usage=D3D11_USAGE_DEFAULT;td.BindFlags=D3D11_BIND_RENDER_TARGET|D3D11_BIND_SHADER_RESOURCE;ComPtr<ID3D11Texture2D>texture;if(FAILED(device->CreateTexture2D(&td,nullptr,&texture)))return false;if(FAILED(device->CreateRenderTargetView(texture.Get(),nullptr,&sceneTarget)))return false;if(FAILED(device->CreateShaderResourceView(texture.Get(),nullptr,&sceneView)))return false;sceneWidth=w;sceneHeight=h;return true;}
    bool Upload(const FrameData& data){D3D11_MAPPED_SUBRESOURCE m{};if(FAILED(context->Map(constants.Get(),0,D3D11_MAP_WRITE_DISCARD,0,&m)))return false;memcpy(m.pData,&data,sizeof(data));context->Unmap(constants.Get(),0);return true;}
    bool Draw(UINT w,UINT h,const FrameData& data){
        if(!Resize(w,h))return false;float scale=std::max(.1f,std::min(1.f,data.renderScale));UINT rw=std::max(1u,(UINT)(w*scale+.5f)),rh=std::max(1u,(UINT)(h*scale+.5f));if(!EnsureSceneTarget(rw,rh))return false;
        FrameData sceneData=data;sceneData.resolution[0]=(float)rw;sceneData.resolution[1]=(float)rh;sceneData.scanlines=0;if(!Upload(sceneData))return false;
        ID3D11RenderTargetView*sceneRt=sceneTarget.Get();context->OMSetRenderTargets(1,&sceneRt,nullptr);D3D11_VIEWPORT sceneVp{0,0,(float)rw,(float)rh,0,1};context->RSSetViewports(1,&sceneVp);context->IASetPrimitiveTopology(D3D11_PRIMITIVE_TOPOLOGY_TRIANGLELIST);context->VSSetShader(vs.Get(),nullptr,0);context->PSSetShader(ps.Get(),nullptr,0);ID3D11Buffer*cb=constants.Get();context->PSSetConstantBuffers(0,1,&cb);context->Draw(3,0);
        ID3D11RenderTargetView*rt=target.Get();context->OMSetRenderTargets(1,&rt,nullptr);D3D11_VIEWPORT vp{0,0,(float)w,(float)h,0,1};context->RSSetViewports(1,&vp);if(!Upload(data))return false;context->PSSetShader(blitPs.Get(),nullptr,0);ID3D11ShaderResourceView*srv=sceneView.Get();context->PSSetShaderResources(0,1,&srv);ID3D11SamplerState*ss=sampler.Get();context->PSSetSamplers(0,1,&ss);context->Draw(3,0);ID3D11ShaderResourceView*none=nullptr;context->PSSetShaderResources(0,1,&none);return SUCCEEDED(swap->Present(1,0));
    }
};

extern "C" __declspec(dllexport) void* __cdecl PlasmaD3D11_Create(HWND hwnd){auto r=new Renderer();if(!r->Init(hwnd)){delete r;return nullptr;}return r;}
extern "C" __declspec(dllexport) int __cdecl PlasmaD3D11_Render(void* handle,int width,int height,const FrameData* data){return handle&&data&&static_cast<Renderer*>(handle)->Draw((UINT)width,(UINT)height,*data)?1:0;}
extern "C" __declspec(dllexport) void __cdecl PlasmaD3D11_Destroy(void* handle){delete static_cast<Renderer*>(handle);}
