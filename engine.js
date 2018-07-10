//chiisa 5/5/2018 nes

//debug
var stats = new Stats();
stats.showPanel(0);
document.body.appendChild(stats.dom);

//render
var gl;
var shaderProgram;
var scene = [];
var modelv = []; //verts
var modeli = []; //indices
var modeln = []; //norms
var modelc = []; //colors
var cam = {x:0,y:0,z:0,a:0,b:0,c:0};
//logic
function voxelize() {
    //use voxelbox to generate strings, then copy them into data.js
    for (var idx = 0; idx < 4; idx++) {
        //var d = data[idx].split("").map(Number); good for 10x10x10 but not bigger
        var d = data[idx].split("").map(n => parseInt(n,36));
        var verts = [];
        var indcs = [];
        var norms = [];
        var colrs = [];

        var mats = [];
        var i,j,c = 0;
        var ver;
        for (i = 1; i < d[0]*3+1; i += 3) {
            mats.push([d[i],d[i+1],d[i+2]]);
        }
        var placeSingle = d.indexOf(35);
        if (placeSingle == -1)
            placeSingle = 99999;
        for (i = d[0]*3+1; i < d.length; i += (i < placeSingle) ? 7 : 4) {
            var x1,y1,z1,x2,y2,z2,mat;
            if (i == placeSingle)
                i++;
            x1 = d[i];
            y1 = d[i+2];
            z1 = d[i+1];
            if (i >= placeSingle) {
                x2 = x1;
                y2 = y1;
                z2 = z1;
                mat = mats[d[i+3]];
            } else {
                x2 = d[i+3];
                y2 = d[i+5];
                z2 = d[i+4];
                mat = mats[d[i+6]];
            }
            ver = createCubeOfDims(0.0+x1/-10+0.4,
                                   0.0+y1/10-0.4,
                                   0.0+z1/10-0.4,
                                   -0.1+x2/-10+0.4,
                                   0.1+y2/10-0.4,
                                   0.1+z2/10-0.4);
            for (j = 0; j < 72; j++) {
                verts[j+(c*72)] = ver[j];
            }
            norms = norms.concat([
                0,0,1,0,0,1,0,0,1,0,0,1,0,0,-1,0,0,-1,0,0,-1,0,0,-1,1,0,0,1,0,0,1,0,0,1,0,0,-1,0,0,-1,0,0,-1,0,0,-1,0,0,0,1,0,0,1,0,0,1,0,0,1,0,0,-1,0,0,-1,0,0,-1,0,0,-1,0
            ]);
            var colConv = 0.125;
            for (j = 0; j < 24; j++) {
                colrs = colrs.concat([
                    mat[0]*colConv,mat[1]*colConv,mat[2]*colConv,1
                ]);
            }
            c++;
        }
        indcs = createIndiciesOfCount(verts.length/6);

        modelv.push(verts);
        modeli.push(indcs);
        modeln.push(norms);
        modelc.push(colrs);
    }
}

function createIndiciesOfCount(count) {
    var array = [];
    for (var i = 0; i < count*4; i+=4) {
        array = array.concat([i,i+1,i+2,i,i+2,i+3]);
    }
    return array;
}

function createCubeOfDims(x1,y1,z1,x2,y2,z2) {
    return [
        //top z+
        x1,y1,z2,
        x2,y1,z2,
        x2,y2,z2,
        x1,y2,z2,
        //bottom z-
        x1,y1,z1,
        x2,y1,z1,
        x2,y2,z1,
        x1,y2,z1,
        //front x+
        x2,y1,z1,
        x2,y2,z1,
        x2,y2,z2,
        x2,y1,z2,
        //back x-
        x1,y2,z1,
        x1,y1,z1,
        x1,y1,z2,
        x1,y2,z2,
        //right y+
        x2,y2,z1,
        x1,y2,z1,
        x1,y2,z2,
        x2,y2,z2,
        //left y-
        x1,y1,z1,
        x2,y1,z1,
        x2,y1,z2,
        x1,y1,z2
    ];
}

//make sure to also change resource meta and the
//renderer code if you end up changing any names
var resources = {
    vert: `attribute vec4 aVertexPosition;
attribute vec3 aVertexNormal;
attribute vec4 aVertexColor;
uniform mat4 uModelViewMatrix;
uniform mat4 uProjectionMatrix;
varying lowp vec4 vColor;
varying highp vec3 vLighting;
void main(void) {
    gl_Position = uProjectionMatrix * uModelViewMatrix * aVertexPosition;

    highp vec3 ambientLight = vec3(0.3, 0.3, 0.3);
    highp vec3 directionalLightColor = vec3(0.4, 0.4, 0.4);
    highp vec3 directionalVector = normalize(vec3(0, 0, 1));
    highp vec4 transformedNormal = vec4(aVertexNormal, 1.0);
    highp float directional = max(dot(mat3(uModelViewMatrix) * transformedNormal.xyz, directionalVector), 0.0);
    vLighting = ambientLight + (directionalLightColor * directional);
    vColor = aVertexColor;
}
`,
    frag: `varying lowp vec4 vColor;
varying highp vec3 vLighting;
void main(void) {
    gl_FragColor = vec4(vColor.rgb * vLighting, vColor.a);
}
`
};
var resourceMeta = {
    aVertexPosition:{},
    aVertexNormal:{},
    aVertexColor:{},
    uModelViewMatrix:{},
    uProjectionMatrix:{}
};

function setup() {
    gl = document.getElementById("c").getContext("webgl2");

    var vertShaderSrc = resources.vert;
    var fragShaderSrc = resources.frag;

    var vertexShader = loadShader(gl.VERTEX_SHADER, vertShaderSrc);
    var fragmentShader = loadShader(gl.FRAGMENT_SHADER, fragShaderSrc);

    shaderProgram = gl.createProgram();
    gl.attachShader(shaderProgram, vertexShader);
    gl.attachShader(shaderProgram, fragmentShader);
    gl.linkProgram(shaderProgram);

    //this won't work with closure so change this to array or separate variables
    Object.keys(resourceMeta).forEach(function (key) {
        if (key.startsWith("a"))
            resourceMeta[key] = gl.getAttribLocation(shaderProgram, key);
        else
            resourceMeta[key] = gl.getUniformLocation(shaderProgram, key);
    });

    setupLock();

    voxelize();

    requestAnimationFrame(render);

    //add scene building here
    addSceneObj(tfm(-2,0,3),0);
    addSceneObj(tfm(0,0,3),1);
    addSceneObj(tfm(2,0,3),2);
    addSceneObj(tfm(4,0,3),3);

    cam.b = Math.PI;
}

//input start
function setupLock() {
    var canvas = document.getElementById("c");
    canvas.requestPointerLock = canvas.requestPointerLock ||
                                canvas.mozRequestPointerLock;

    document.exitPointerLock = document.exitPointerLock ||
                               document.mozExitPointerLock;

    canvas.onclick = function() {
        canvas.requestPointerLock();
    };

    document.addEventListener("pointerlockchange", lockChangeAlert, false);
    document.addEventListener("mozpointerlockchange", lockChangeAlert, false);

    document.onkeydown = handleKeyDown;
    document.onkeyup = handleKeyUp;

}

function lockChangeAlert() {
    var canvas = document.getElementById("c");
    if (document.pointerLockElement === canvas ||
        document.mozPointerLockElement === canvas) {
        document.addEventListener("mousemove", updatePosition, false);
    } else {
        document.removeEventListener("mousemove", updatePosition, false);
    }
}

function updatePosition(e) {
    cam.b -= e.movementX/100;
    cam.a -= e.movementY/100;
}

var keysDown = {};
function handleKeyDown(event) {
    keysDown[event.keyCode] = true;
}

function handleKeyUp(event) {
    keysDown[event.keyCode] = false;
}

function handleKeys() {
    //put key handling here
    if (keysDown[37] || keysDown[65]) {
        move(0.01,180);
    } else if (keysDown[39] || keysDown[68]) {
        move(0.01,0);
    }

    if (keysDown[38] || keysDown[87]) {
        move(0.01,-90);
    } else if (keysDown[40] || keysDown[83]) {
        move(0.01,90);
    }

    if (keysDown[32]) {
        cam.y += 0.01;
    } else if (keysDown[16] || keysDown[67]) {
        cam.y -= 0.01;
    }
}

function move(len, deg) {
    cam.x += len * Math.cos(((-cam.b*180/Math.PI+deg)%360)*Math.PI/180);
    cam.z += len * Math.sin(((-cam.b*180/Math.PI+deg)%360)*Math.PI/180);
}
//input end

function render() {
    stats.begin();

    gl.clearColor(0, 0, 0, 1);
    gl.clearDepth(1);
    gl.enable(gl.DEPTH_TEST);
    gl.depthFunc(gl.LEQUAL);
    gl.clear(gl.COLOR_BUFFER_BIT | gl.DEPTH_BUFFER_BIT);

    handleKeys();

    scene.forEach(function(obj) {
        renderObj(obj);
    });

    stats.end();

    requestAnimationFrame(render);
}

function renderObj(obj) {
    var proj = new Float32Array(persp(gl.canvas.clientWidth / gl.canvas.clientHeight, 0.1, 100));
    var vm = new Float32Array(lookAtFps([cam.x-obj.tfm.x,cam.y-obj.tfm.y,cam.z-obj.tfm.z],cam.a,cam.b));
    enableBuffer(resourceMeta.aVertexPosition, obj.pos, 3);
    enableBuffer(resourceMeta.aVertexNormal, obj.nrm, 3);
    enableBuffer(resourceMeta.aVertexColor, obj.col, 4);
    gl.bindBuffer(gl.ELEMENT_ARRAY_BUFFER, obj.idx);
    gl.useProgram(shaderProgram);
    enableBuffer(resourceMeta.uProjectionMatrix, proj);
    enableBuffer(resourceMeta.uModelViewMatrix, vm);
    gl.drawElements(gl.TRIANGLES, obj.vct/2, gl.UNSIGNED_SHORT, 0);
}

function enableBuffer(attr, buff, compCount = undefined/*optional*/) {
    if (compCount !== undefined) {
        gl.bindBuffer(gl.ARRAY_BUFFER, buff);
        gl.vertexAttribPointer(attr, compCount, gl.FLOAT, false, 0, 0);
        gl.enableVertexAttribArray(attr);
    } else {
        gl.uniformMatrix4fv(attr, false, buff);
    }
}

function addSceneObj(transform,modelIndex/*,shader*/) {
    var positionBuffer = gl.createBuffer();
    gl.bindBuffer(gl.ARRAY_BUFFER, positionBuffer);
    gl.bufferData(gl.ARRAY_BUFFER, new Float32Array(modelv[modelIndex]), gl.STATIC_DRAW);

    var indexBuffer = gl.createBuffer();
    gl.bindBuffer(gl.ELEMENT_ARRAY_BUFFER, indexBuffer);
    gl.bufferData(gl.ELEMENT_ARRAY_BUFFER, new Uint16Array(modeli[modelIndex]), gl.STATIC_DRAW);

    var normalBuffer = gl.createBuffer();
    gl.bindBuffer(gl.ARRAY_BUFFER, normalBuffer);
    gl.bufferData(gl.ARRAY_BUFFER, new Float32Array(modeln[modelIndex]), gl.STATIC_DRAW);

    var colorBuffer = gl.createBuffer();
    gl.bindBuffer(gl.ARRAY_BUFFER, colorBuffer);
    gl.bufferData(gl.ARRAY_BUFFER, new Float32Array(modelc[modelIndex]), gl.STATIC_DRAW);

    scene.push({
        tfm: transform,
        pos: positionBuffer,
        vct: modelv[modelIndex].length,
        idx: indexBuffer,
        nrm: normalBuffer,
        col: colorBuffer
    });
}
function removeSceneObj(obj) {

}

//utils
//shaders
function loadShader(type, source) {
    var shader = gl.createShader(type);
    gl.shaderSource(shader, source);
    gl.compileShader(shader);
    return shader;
}
//matrices
function transform(posX, posY, posZ, rotX, rotY, rotZ) {
    var a = Math.cos(rotX);
    var b = Math.sin(rotX);
    var c = Math.cos(rotY);
    var d = Math.sin(rotY);
    var e = Math.cos(rotZ);
    var f = Math.sin(rotZ);
    return [c*e,c*f,-d,0,
           -a*f+b*d*e,a*e+b*d*f,b*c,0,
            a*d*e+b*f,a*d*f-b*e,a*c,0,
            posX,posY,posZ,1];
}
//https://www.3dgep.com/understanding-the-view-matrix/
//"The function to implement this camera model might look like this:"
function lookAtFps(eye, pitch, yaw) {
    var cosPitch = Math.cos(((pitch+90)%180)-90);
    var sinPitch = Math.sin(((pitch+90)%180)-90);
    var cosYaw = Math.cos(yaw%360);
    var sinYaw = Math.sin(yaw%360);

    var xaxis = [cosYaw, 0, -sinYaw];
    var yaxis = [sinYaw * sinPitch, cosPitch, cosYaw * sinPitch];
    var zaxis = [sinYaw * cosPitch, -sinPitch, cosPitch * cosYaw];

    return [
        xaxis[0],yaxis[0],zaxis[0],0,
        xaxis[1],yaxis[1],zaxis[1],0,
        xaxis[2],yaxis[2],zaxis[2],0,
        -dot3(xaxis,eye),-dot3(yaxis,eye),-dot3(zaxis,eye),1
    ];
}
function dot3(u, v) {
    return u[0]*v[0]+u[1]*v[1]+u[2]*v[2];
}
function cross(u, v) {
    return [u[1]*v[2]-u[2]*v[1],u[2]*v[0]-u[0]*v[2],u[0]*v[1]-u[1]*v[0]];
}
function norm(u) {
    var l = Math.sqrt((u[0]*u[0])+(u[1]*u[1])+(u[2]*u[2]));
    if (l > 0)
        return [u[0]/l,u[1]/l,u[2]/l];
    else
        return [0,0,0];
}

//https://webgl2fundamentals.org/webgl/lessons/webgl-3d-perspective.html
//"Here's a function to build the matrix."
function persp(aspect, near, far) {
    var fov = 0.7854;
    var f = Math.tan(Math.PI * 0.5 - 0.5 * fov);
    var rangeInv = 1.0 / (near - far);
    return [
        f/aspect,0,0,0,
        0,f,0,0,
        0,0,(near+far)*rangeInv,-1,
        0,0,near*far*rangeInv*2,0
    ];
}
//bridges
function tfm(posX, posY, posZ, rotX = 0, rotY = 0, rotZ = 0) {
    return {
        x: posX,
        y: posY,
        z: posZ,
        a: rotX,
        b: rotY,
        c: rotZ
    }
}

setup();
