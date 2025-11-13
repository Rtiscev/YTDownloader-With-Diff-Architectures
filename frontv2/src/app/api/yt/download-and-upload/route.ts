import { NextRequest, NextResponse } from 'next/server'

const BACKEND_DOWNLOAD_URL = 'http://apigateway:5000/api/ytdlp/download-and-upload'

interface DownloadRequest {
    url: string
    format?: string
    extractAudio: boolean
    audioFormat?: string
    audioQuality?: string
    mergeAudio?: boolean
    resolution?: string
    outputPath?: string
}

export async function POST(req: NextRequest) {
    try {
        const body: DownloadRequest = await req.json()

        if (!body.url) {
            return NextResponse.json(
                { success: false, error: 'Missing url in request body' },
                { status: 400 }
            )
        }

        const token = req.headers.get('authorization')?.replace('Bearer ', '') ||
            req.cookies.get('accessToken')?.value

        const backendRes = await fetch(BACKEND_DOWNLOAD_URL, {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
                ...(token && { 'Authorization': `Bearer ${token}` }),
            },
            body: JSON.stringify({
                url: body.url,
                format: body.format,
                extractAudio: body.extractAudio,
                audioFormat: body.audioFormat,
                audioQuality: body.audioQuality,
                resolution: body.resolution,
                mergeAudio: body.mergeAudio ?? true,
                outputPath: body.outputPath,
            }),
        })

        const data = await backendRes.json()

        if (backendRes.status === 401) {
            return NextResponse.json(
                { success: false, error: 'Please login to download this quality' },
                { status: 401 }
            )
        }

        if (backendRes.status === 403) {
            return NextResponse.json(
                { success: false, error: 'This quality requires a premium subscription' },
                { status: 403 }
            )
        }

        if (!backendRes.ok) {
            return NextResponse.json(data, { status: backendRes.status })
        }

        return NextResponse.json(data)
    } catch (error) {
        console.error('[Download-Upload Proxy] Error:', error)
        return NextResponse.json(
            { success: false, error: 'Failed to reach download service' },
            { status: 500 }
        )
    }
}
