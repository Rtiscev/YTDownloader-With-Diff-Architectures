import { NextRequest, NextResponse } from 'next/server'

export async function GET(
    req: NextRequest,
    { params }: { params: Promise<{ bucketName: string; objectName: string }> }
) {
    try {
        const { bucketName, objectName } = await params

        if (!bucketName || !objectName) {
            return NextResponse.json({ error: 'Invalid parameters' }, { status: 400 })
        }

        const decodedObjectName = decodeURIComponent(objectName)

        const backendRes = await fetch('http://apigateway:5000/api/minioservice/download', {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({
                bucket: bucketName,
                object: decodedObjectName,
            }),
        })

        if (!backendRes.ok) {
            return NextResponse.json(
                { error: 'File not found' },
                { status: backendRes.status }
            )
        }

        const fileBuffer = await backendRes.arrayBuffer()

        return new NextResponse(fileBuffer, {
            status: 200,
            headers: {
                'Content-Type': 'application/octet-stream',
                'Content-Disposition': `attachment; filename="${decodedObjectName}"`,
                'Content-Length': fileBuffer.byteLength.toString(),
            },
        })
    } catch (error) {
        console.error('[MinIO Download] Error:', error)
        return NextResponse.json({ error: 'Failed to download file' }, { status: 500 })
    }
}
