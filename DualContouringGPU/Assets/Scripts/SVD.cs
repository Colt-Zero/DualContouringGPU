using UnityEngine;
using System.Collections;

public class SMat3
{
	public float m00, m01, m02, m11, m12, m22;

	public SMat3()
	{
		this.clear();
	}

	public SMat3(float m00, float m01, float m02,
	             float m11, float m12, float m22)
	{
		this.setSymmetric(m00, m01, m02, m11, m12, m22);
	}

	public void clear()
	{
		this.setSymmetric(0, 0, 0, 0, 0, 0);
	}

	public void setSymmetric(float a00, float a01, float a02,
	                  float a11, float a12, float a22)
	{
		this.m00 = a00;
		this.m01 = a01;
		this.m02 = a02;
		this.m11 = a11;
		this.m12 = a12;
		this.m22 = a22;
	}

	public void setSymmetric(SMat3 rhs)
	{
		this.setSymmetric(rhs.m00, rhs.m01, rhs.m02, rhs.m11, rhs.m12, rhs.m22);
	}

	private SMat3(SMat3 rhs)
	{
		this.setSymmetric(rhs);
	}
}

public class Mat3
{
	public float m00, m01, m02, m10, m11, m12, m20, m21, m22;

	public Mat3()
	{
		this.clear();
	}

	public Mat3(float m00, float m01, float m02,
	            float m10, float m11, float m12,
	            float m20, float m21, float m22)
	{
		this.set(m00, m01, m02, m10, m11, m12, m20, m21, m22);
	}

	public void clear()
	{
		this.set(0, 0, 0, 0, 0, 0, 0, 0, 0);
	}

	public void set(float m00, float m01, float m02,
	                float m10, float m11, float m12,
	                float m20, float m21, float m22)
	{
		this.m00 = m00;
		this.m01 = m01;
		this.m02 = m02;
		this.m10 = m10;
		this.m11 = m11;
		this.m12 = m12;
		this.m20 = m20;
		this.m21 = m21;
		this.m22 = m22;
	}


	public void set(Mat3 rhs)
	{
		this.set(rhs.m00, rhs.m01, rhs.m02, rhs.m10, rhs.m11, rhs.m12, rhs.m20,
		         rhs.m21, rhs.m22);
	}

	public void setSymmetric(float a00, float a01, float a02,
	                         float a11, float a12, float a22)
	{
		this.set(a01, a01, a02, a01, a11, a12, a02, a12, a22);
	}

	public void setSymmetric(SMat3 rhs)
	{
		this.setSymmetric(rhs.m00, rhs.m01, rhs.m02, rhs.m11, rhs.m12, rhs.m22);
	}

	private Mat3(Mat3 rhs)
	{
		this.set(rhs);
	}
}

public class Vec3
{
	public float x, y, z;

	public Vec3()
	{
		this.clear();
	}

	public Vec3(float x, float y, float z)
	{
		this.set(x, y, z);
	}

	public void clear()
	{
		this.set(0, 0, 0);
	}

	public void set(float x, float y, float z)
	{
		this.x = x;
		this.y = y;
		this.z = z;
	}

	public void set(Vec3 rhs)
	{
		this.set(rhs.x, rhs.y, rhs.y);
	}

	private Vec3(Vec3 rhs)
	{
		this.set(rhs);
	}
}

class MatUtils
{
	public static float fnorm(Mat3 a)
	{
		return Mathf.Sqrt((a.m00 * a.m00) + (a.m01 * a.m01) + (a.m02 * a.m02)
		                  + (a.m10 * a.m10) + (a.m11 * a.m11) + (a.m12 * a.m12)
		                  + (a.m20 * a.m20) + (a.m21 * a.m21) + (a.m22 * a.m22));
	}

	public static float fnorm(SMat3 a)
	{
		return Mathf.Sqrt((a.m00 * a.m00) + (a.m01 * a.m01) + (a.m02 * a.m02)
		                  + (a.m01 * a.m01) + (a.m11 * a.m11) + (a.m12 * a.m12)
		                  + (a.m02 * a.m02) + (a.m12 * a.m12) + (a.m22 * a.m22));
	}

	public static float off(Mat3 a)
	{
		return Mathf.Sqrt((a.m01 * a.m01) + (a.m02 * a.m02) + (a.m10 * a.m10)
		                  + (a.m12 * a.m12) + (a.m20 * a.m20) + (a.m21 * a.m21));
	}

	public static float off(SMat3 a)
	{
		return Mathf.Sqrt(2.0f * ((a.m01 * a.m01) + (a.m02 * a.m02) + (a.m12 * a.m12)));
	}

	public static void mmul(out Mat3 mout, Mat3 a, Mat3 b)
	{
		mout = new Mat3(a.m00 * b.m00 + a.m01 * b.m10 + a.m02 * b.m20,
				        a.m00 * b.m01 + a.m01 * b.m11 + a.m02 * b.m21,
				        a.m00 * b.m02 + a.m01 * b.m12 + a.m02 * b.m22,
				        a.m10 * b.m00 + a.m11 * b.m10 + a.m12 * b.m20,
				        a.m10 * b.m01 + a.m11 * b.m11 + a.m12 * b.m21,
				        a.m10 * b.m02 + a.m11 * b.m12 + a.m12 * b.m22,
				        a.m20 * b.m00 + a.m21 * b.m10 + a.m22 * b.m20,
				        a.m20 * b.m01 + a.m21 * b.m11 + a.m22 * b.m21,
				        a.m20 * b.m02 + a.m21 * b.m12 + a.m22 * b.m22);
	}

	public static void mmul_ata(out SMat3 mout, Mat3 a)
	{
		mout = new SMat3(a.m00 * a.m00 + a.m10 * a.m10 + a.m20 * a.m20,
		                 a.m00 * a.m01 + a.m10 * a.m11 + a.m20 * a.m21,
		                 a.m00 * a.m02 + a.m10 * a.m12 + a.m20 * a.m22,
		                 a.m01 * a.m01 + a.m11 * a.m11 + a.m21 * a.m21,
		                 a.m01 * a.m02 + a.m11 * a.m12 + a.m21 * a.m22,
		                 a.m02 * a.m02 + a.m12 * a.m12 + a.m22 * a.m22);
	}

	public static void transpose(out Mat3 mout, Mat3 a)
	{
		mout = new Mat3(a.m00, a.m10, a.m20, a.m01, a.m11, a.m21, a.m02, a.m12, a.m22);
	}

	public static void vmul(out Vec3 vout, Mat3 a, Vec3 v)
	{
		vout = new Vec3();
		vout.x = (a.m00 * v.x) + (a.m01 * v.y) + (a.m02 * v.z);
		vout.y = (a.m10 * v.x) + (a.m11 * v.y) + (a.m12 * v.z);
		vout.z = (a.m20 * v.x) + (a.m21 * v.y) + (a.m22 * v.z);
	}

	public static void vmul_symmetric(out Vec3 vout, SMat3 a, Vec3 v)
	{
		vout = new Vec3();
		vout.x = (a.m00 * v.x) + (a.m01 * v.y) + (a.m02 * v.z);
		vout.y = (a.m01 * v.x) + (a.m11 * v.y) + (a.m12 * v.z);
		vout.z = (a.m02 * v.x) + (a.m12 * v.y) + (a.m22 * v.z);
	}
}

class VecUtils
{
	public static void addScaled(out Vec3 v, float s, Vec3 rhs)
	{
		v = new Vec3();
		v.x += s * rhs.x;
		v.y += s * rhs.y;
		v.z += s * rhs.z;
	}

	public static float dot(Vec3 a, Vec3 b)
	{
		return a.x * b.x + a.y * b.y + a.z * b.z;
	}

	public static void normalize(out Vec3 v, Vec3 iv)
	{
		v = iv;
		float len2 = dot(v, v);

		if (Mathf.Abs(len2) < 1e-12)
		{
			v.clear();
		}
		else
		{
			scale(out v, 1.0f / Mathf.Sqrt(len2), v);
		}
	}

	public static void scale(out Vec3 v, float s, Vec3 iv)
	{
		v = iv;
		v.x *= s;
		v.y *= s;
		v.z *= s;
	}

	public static void sub(out Vec3 c, Vec3 a, Vec3 b)
	{
		float v0 = a.x - b.x;
		float v1 = a.y - b.y;
		float v2 = a.z - b.z;
		c = new Vec3();
		c.x = v0;
		c.y = v1;
		c.z = v2;
	}
}

public class Givens
{
	public static void rot01_post(out Mat3 m, float c, float s, Mat3 im)
	{
		m = im;
		float m00 = m.m00, m01 = m.m01, m10 = m.m10, m11 = m.m11, m20 = m.m20,
					m21 = m.m21;
		m.set(c * m00 - s * m01, s * m00 + c * m01, m.m02, c * m10 - s * m11,
		      s * m10 + c * m11, m.m12, c * m20 - s * m21, s * m20 + c * m21, m.m22);
	}

	public static void rot02_post(out Mat3 m, float c, float s, Mat3 im)
	{
		m = im;
		float m00 = m.m00, m02 = m.m02, m10 = m.m10, m12 = m.m12,
					m20 = m.m20, m22 = m.m22;
		m.set(c * m00 - s * m02, m.m01, s * m00 + c * m02, c * m10 - s * m12, m.m11,
		      s * m10 + c * m12, c * m20 - s * m22, m.m21, s * m20 + c * m22);
	}

	public static void rot12_post(out Mat3 m, float c, float s, Mat3 im)
	{
		m = im;
		float m01 = m.m01, m02 = m.m02, m11 = m.m11, m12 = m.m12,
					m21 = m.m21, m22 = m.m22;
		m.set(m.m00, c * m01 - s * m02, s * m01 + c * m02, m.m10, c * m11 - s * m12,
		      s * m11 + c * m12, m.m20, c * m21 - s * m22, s * m21 + c * m22);
	}
}

public class Schur2
{
	private static void calcSymmetricGivensCoefficients(float a_pp, float a_pq,
	                                                    float a_qq, out float c,
	                                                    out float s)
	{
		if (a_pq == 0)
		{
			c = 1;
			s = 0;
			return;
		}

		float tau = (a_qq - a_pp) / (2.0f * a_pq);
		float stt = Mathf.Sqrt(1.0f + tau * tau);
		float tan = 1.0f / ((tau >= 0) ? (tau + stt) : (tau - stt));
		c = 1.0f / Mathf.Sqrt (1.0f + tan * tan);
		s = tan * c;
	}

	public static void rot01(out SMat3 m, out float c, out float s, SMat3 im, float Ic, float Is)
	{
		m = im; c = Ic; s = Is;
		calcSymmetricGivensCoefficients(m.m00, m.m01, m.m11, out c, out s);
		float cc = c * c;
		float ss = s * s;
		float mix = 2.0f * c * s * m.m01;
		m.setSymmetric(cc * m.m00 - mix + ss * m.m11, 0, c * m.m02 - s * m.m12,
		               ss * m.m00 + mix + cc * m.m11, s * m.m02 + c * m.m12, m.m22);
	}

	public static void rot02(out SMat3 m, out float c, out float s, SMat3 im, float Ic, float Is)
	{
		m = im; c = Ic; s = Is;
		calcSymmetricGivensCoefficients(m.m00, m.m02, m.m22, out c, out s);
		float cc = c * c;
		float ss = s * s;
		float mix = 2.0f * c * s * m.m02;
		m.setSymmetric(cc * m.m00 - mix + ss * m.m22, c * m.m01 - s * m.m12, 0,
		               m.m11, s * m.m01 + c * m.m12, ss * m.m00 + mix + cc * m.m22);
	}

	public static void rot12(out SMat3 m, out float c, out float s, SMat3 im, float Ic, float Is)
	{
		m = im; c = Ic; s = Is;
		calcSymmetricGivensCoefficients(m.m11, m.m12, m.m22, out c, out s);
		float cc = c * c;
		float ss = s * s;
		float mix = 2.0f * c * s * m.m12;
		m.setSymmetric(m.m00, c * m.m01 - s * m.m02, s * m.m01 + c * m.m02,
		               cc * m.m11 - mix + ss * m.m22, 0, ss * m.m11 + mix + cc * m.m22);
	}

	public static void rotate01(out SMat3 vtav, out Mat3 v, SMat3 ivtav, Mat3 iv)
	{
		vtav = ivtav;
		v = iv;
		if (vtav.m01 == 0)
			return;

		float c, s;
		rot01(out vtav, out c, out s, vtav, c, s);
		Givens.rot01_post(out v, c, s, v);
	}

	public static void rotate02(out SMat3 vtav, out Mat3 v, SMat3 ivtav, Mat3 iv)
	{
		vtav = ivtav;
		v = iv;
		if (vtav.m02 == 0)
			return;

		float c, s;
		rot02(out vtav, out c, out s, vtav, c, s);
		Givens.rot02_post(out v, c, s, v);
	}

	public static void rotate12(out SMat3 vtav, out Mat3 v, SMat3 ivtav, Mat3 iv)
	{
		vtav = ivtav;
		v = iv;
		if (vtav.m12 == 0)
			return;

		float c, s;
		rot12(out vtav, out c, out s, vtav, c, s);
		Givens.rot12_post(out v, c, s, v);
	}
}

public class Svd
{
	private static float calcError(Mat3 A, Vec3 x, Vec3 b)
	{
		Vec3 vtmp;
		MatUtils.vmul(out vtmp, A, x);
		VecUtils.sub(out vtmp, b, vtmp);
		return VecUtils.dot(vtmp, vtmp);
	}

	private static float calcError(SMat3 origA, Vec3 x, Vec3 b)
	{
		Mat3 A = new Mat3();
		Vec3 vtmp;
		A.setSymmetric(origA);
		MatUtils.vmul(out vtmp, A, x);
		VecUtils.sub(out vtmp, b, vtmp);
		return VecUtils.dot(vtmp, vtmp);
	}

	private static float pinv(float x, float tol)
	{
		return (Mathf.Abs(x) < tol || Mathf.Abs(1.0f / x) < tol) ? 0 : (1.0f / x);
	}

	public static void getSymmetricSvd(SMat3 a, out SMat3 vtav, out Mat3 v,
	                                   float tol, int max_sweeps)
	{
		vtav = new SMat3();
		v = new Mat3();
		vtav.setSymmetric(a);
		v.set(1, 0, 0, 0, 1, 0, 0, 0, 1);
		float delta = tol * MatUtils.fnorm(vtav);

		for (int i = 0; i < max_sweeps && MatUtils.off(vtav) > delta; ++i)
		{
			Schur2.rotate01(out vtav, out v, vtav, v);
			Schur2.rotate02(out vtav, out v, vtav, v);
			Schur2.rotate12(out vtav, out v, vtav, v);
		}
	}

	public static void psuedoinverse(out Mat3 m, SMat3 d, Mat3 v, float tol)
	{
		m = new Mat3();

		float d0 = pinv(d.m00, tol), d1 = pinv (d.m11, tol), d2 = pinv(d.m22, tol);

		m.set(v.m00 * d0 * v.m00 + v.m01 * d1 * v.m01 + v.m02 * d2 * v.m02,
		      v.m00 * d0 * v.m10 + v.m01 * d1 * v.m11 + v.m02 * d2 * v.m12,
		      v.m00 * d0 * v.m20 + v.m01 * d1 * v.m21 + v.m02 * d2 * v.m22,
		      v.m10 * d0 * v.m00 + v.m11 * d1 * v.m01 + v.m12 * d2 * v.m02,
		      v.m10 * d0 * v.m10 + v.m11 * d1 * v.m11 + v.m12 * d2 * v.m12,
		      v.m10 * d0 * v.m20 + v.m11 * d1 * v.m21 + v.m12 * d2 * v.m22,
		      v.m20 * d0 * v.m00 + v.m21 * d1 * v.m01 + v.m22 * d2 * v.m02,
		      v.m20 * d0 * v.m10 + v.m21 * d1 * v.m11 + v.m22 * d2 * v.m12,
		      v.m20 * d0 * v.m20 + v.m21 * d1 * v.m21 + v.m22 * d2 * v.m22);
	}

	public static float solveSymmetric(SMat3 A, Vec3 b, out Vec3 x,
	                                   float svd_tol, int svd_sweeps, float pinv_tol)
	{
		Mat3 mtmp, pinv, V;
		SMat3 VTAV;
		getSymmetricSvd(A, out VTAV, out V, svd_tol, svd_sweeps);
		psuedoinverse(out pinv, VTAV, V, pinv_tol);
		MatUtils.vmul(out x, pinv, b);
		return calcError(A, x, b);
	}
}

public class LeastSquares
{
	public static float solveLeastSquares(Mat3 a, Vec3 b, out Vec3 x,
	                                      float svd_tol, int svd_sweeps, float pinv_tol)
	{
		Mat3 at;
		SMat3 ata;
		Vec3 atb;
		MatUtils.transpose(out at, a);
		MatUtils.mmul_ata(out ata, a);
		MatUtils.vmul(out atb, at, b);
		return Svd.solveSymmetric(ata, atb, out x, svd_tol, svd_sweeps, pinv_tol);
	}
}
