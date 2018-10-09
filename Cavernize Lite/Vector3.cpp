#include "Vector3.h"

Vector3::Vector3(float xin, float yin, float zin) {
    x = xin;
    y = yin;
    z = zin;
}

Vector3 placeInSphere(Vector3 angles) {
	float XRad = angles.x * deg2Rad, YRad = angles.y * deg2Rad, SinX = sinf(XRad), CosX = cosf(XRad),
		SinY = sinf(YRad), CosY = cosf(YRad);
	return Vector3(SinY * CosX, -SinX, CosY * CosX);
}

Vector3 placeInCube(Vector3 angles) {
	float XRad = angles.x * deg2Rad, YRad = angles.y * deg2Rad, SinX = sinf(XRad), CosX = cosf(XRad),
		SinY = sinf(YRad), CosY = cosf(YRad);
	if (fabsf(SinY) > fabsf(CosY))
		SinY = SinY > 0 ? sqrt2p2 : sqrt2pm2;
	else
		CosY = CosY > 0 ? sqrt2p2 : sqrt2pm2;
	SinY /= sqrt2p2;
	CosY /= sqrt2p2;
	if (fabsf(SinX) >= sqrt2p2) {
		SinX = SinX > 0 ? sqrt2p2 : sqrt2pm2;
		CosX /= sqrt2p2;
		SinY *= CosX;
		CosY *= CosX;
	}
	SinX /= sqrt2p2;
	return Vector3(SinY, -SinX, CosY);
}

inline float lerp(float a, float b, float t) {
	return (b - a) * t + a;
}

Vector3 lerp(Vector3 a, Vector3 b, float t) {
	return Vector3(
		(b.x - a.x) * t + a.x,
		(b.y - a.y) * t + a.y,
		(b.z - a.z) * t + a.z
	);
}
