using UnityEngine;
using System.Collections;

public class QefData
{
	public float ata_00, ata_01, ata_02, ata_11, ata_12, ata_22;
	public float atb_x, atb_y, atb_z;
	public float btb;

	public float massPoint_x, massPoint_y, massPoint_z;
	public int numPoints;

	public QefData()
	{
		this.clear();
	}

	public QefData(float ata_00, float ata_01,
	        float ata_02, float ata_11, float ata_12,
	        float ata_22, float atb_x, float atb_y,
	        float atb_z, float btb, float massPoint_x,
	        float massPoint_y, float massPoint_z,
	        int numPoints)
	{
		this.set(ata_00, ata_01, ata_02, ata_11, ata_12, ata_22, atb_x, atb_y,
		         atb_z, btb, massPoint_x, massPoint_y, massPoint_z, numPoints);
	}

	public QefData(QefData rhs)
	{
		this.set(rhs);
	}

	public void add(QefData rhs)
	{
		this.ata_00 += rhs.ata_00;
		this.ata_01 += rhs.ata_01;
		this.ata_02 += rhs.ata_02;
		this.ata_11 += rhs.ata_11;
		this.ata_12 += rhs.ata_12;
		this.ata_22 += rhs.ata_22;
		this.atb_x += rhs.atb_x;
		this.atb_y += rhs.atb_y;
		this.atb_y += rhs.atb_z;
		this.btb += rhs.btb;
		this.massPoint_x += rhs.massPoint_x;
		this.massPoint_y += rhs.massPoint_y;
		this.massPoint_z += rhs.massPoint_z;
		this.numPoints += rhs.numPoints;
	}

	public void clear()
	{
		this.set(0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0);
	}

	public void set(float ata_00, float ata_01,
	         float ata_02, float ata_11, float ata_12,
	         float ata_22, float atb_x, float atb_y,
	         float atb_z, float btb, float massPoint_x,
	         float massPoint_y, float massPoint_z,
	         int numPoints)
	{
		this.ata_00 = ata_00;
		this.ata_01 = ata_01;
		this.ata_02 = ata_02;
		this.ata_11 = ata_11;
		this.ata_12 = ata_12;
		this.ata_22 = ata_22;
		this.atb_x = atb_x;
		this.atb_y = atb_y;
		this.atb_z = atb_z;
		this.btb = btb;
		this.massPoint_x = massPoint_x;
		this.massPoint_y = massPoint_y;
		this.massPoint_z = massPoint_z;
		this.numPoints = numPoints;
	}

	public void set(QefData rhs)
	{
		this.set(rhs.ata_00, rhs.ata_01, rhs.ata_02, rhs.ata_11, rhs.ata_12,
		         rhs.ata_22, rhs.atb_x, rhs.atb_y, rhs.atb_z, rhs.btb,
		         rhs.massPoint_x, rhs.massPoint_y, rhs.massPoint_z,
		         rhs.numPoints);
	}
}

public class QefSolver
{
	private QefData data;
	private SMat3 ata;
	private Vec3 atb, massPoint, x;
	private bool hasSolution;

	public QefSolver()
	{
		data = new QefData();
		ata = new SMat3();
		atb = new Vec3();
		massPoint = new Vec3();
		x = new Vec3();
		hasSolution = false;
	}

	private static void normalize(out float nx, out float ny, out float nz, float ix, float iy, float iz)
	{
		Vec3 tmpv = new Vec3(ix, iy, iz);
		VecUtils.normalize(out tmpv, tmpv);
		nx = tmpv.x;
		ny = tmpv.y;
		nz = tmpv.z;
	}

	public Vec3 getMassPoint() { return massPoint; }

	public void add(float px, float py, float pz,
	         float nx, float ny, float nz)
	{
		this.hasSolution = false;
		normalize(out nx, out ny, out nz, nx, ny, nz);
		this.data.ata_00 += nx * nx;
		this.data.ata_01 += nx * ny;
		this.data.ata_02 += nx * nz;
		this.data.ata_11 += ny * ny;
		this.data.ata_12 += ny * nz;
		this.data.ata_22 += nz * nz;

		float dot = nx * px + ny * py + nz * pz;
		this.data.atb_x += dot * nx;
		this.data.atb_y += dot * ny;
		this.data.atb_z += dot * nz;
		this.data.btb += dot * dot;
		this.data.massPoint_x += px;
		this.data.massPoint_y += py;
		this.data.massPoint_z += pz;
		++this.data.numPoints;
	}

	public void add(Vec3 p, Vec3 n)
	{
		this.add(p.x, p.y, p.z, n.x, n.y, n.z);
	}

	public void add(QefData rhs)
	{
		this.hasSolution = false;
		this.data.add(rhs);
	}

	public QefData getData()
	{
		return data;
	}

	public float getError()
	{
		if (!this.hasSolution)
		{
			throw new UnityException("Illegal state");
		}

		return this.getError(this.x);
	}

	public float getError(Vec3 pos)
	{
		if (!this.hasSolution)
		{
			this.setAta();
			this.setAtb();
		}

		Vec3 atax;
		MatUtils.vmul_symmetric(out atax, this.ata, pos);
		return VecUtils.dot(pos, atax) - 2.0f * VecUtils.dot(pos, this.atb)
				+ this.data.btb;
	}

	public void reset()
	{
		this.hasSolution = false;
		this.data.clear();
	}

	public float solve(out Vec3 outx, float svd_tol,
	                   int svd_sweeps, float pinv_tol)
	{
		if (this.data.numPoints == 0)
		{
			throw new UnityException("...");
		}

		this.massPoint.set(this.data.massPoint_x, this.data.massPoint_y, this.data.massPoint_z);
		VecUtils.scale(out this.massPoint, 1.0f / this.data.numPoints, this.massPoint);

		this.setAta();
		this.setAtb();

		Vec3 tmpv;
		MatUtils.vmul_symmetric(out tmpv, this.ata, this.massPoint);
		VecUtils.sub(out this.atb, this.atb, tmpv);

		this.x.clear();
		float result = Svd.solveSymmetric(this.ata, this.atb, out this.x,
		                                  svd_tol, svd_sweeps, pinv_tol);
		VecUtils.addScaled(out this.x, 1.0f, this.massPoint);

		this.setAtb();
		outx = x;
		this.hasSolution = true;
		return result;
	}

	private void setAta()
	{
		this.ata.setSymmetric(this.data.ata_00, this.data.ata_01,
		                      this.data.ata_02, this.data.ata_11,
		                      this.data.ata_12, this.data.ata_22);
	}

	private void setAtb()
	{
		this.atb.set(this.data.atb_x, this.data.atb_y, this.data.atb_z);
	}
}
