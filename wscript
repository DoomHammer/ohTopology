#!/usr/bin/python

import sys
import os

def options(opt):
    opt.load('msvc')
    opt.load('compiler_cxx')
    opt.add_option('--ohnet-include-dir', action='store', default=None)
    opt.add_option('--ohnet-lib-dir', action='store', default=None)
    opt.add_option('--ohnet', action='store', default=None)
    opt.add_option('--debug', action='store_const', dest="debugmode", const="Debug", default="Release")
    opt.add_option('--release', action='store_const', dest="debugmode",  const="Release", default="Release")
    opt.add_option('--dest-platform', action='store', default=None)
    opt.add_option('--cross', action='store', default=None)
    #opt.add_option('--big-endian', action='store_const', dest="endian",  const="BIG", default="LITTLE")
    #opt.add_option('--little-endian', action='store_const', dest="endian",  const="LITTLE", default="LITTLE")
    #opt.add_option('--dest', action='store', default=None)

platforms = {
        'Linux-x86': dict(endian='LITTLE',   build_platform='linux2', ohnet_plat_dir='Posix'),
        'Linux-x64': dict(endian='LITTLE',   build_platform='linux2', ohnet_plat_dir='Posix'),
        'Linux-ARM': dict(endian='LITTLE',   build_platform='linux2', ohnet_plat_dir='Posix'),
        'Windows-x86': dict(endian='LITTLE', build_platform='win32',  ohnet_plat_dir='Windows'),
        'Windows-x64': dict(endian='LITTLE', build_platform='win32',  ohnet_plat_dir='Windows'),
        'Core': dict(endian='BIG',           build_platform='linux2', ohnet_plat_dir='Volcano2'),
        'Mac-x86': dict(endian='LITTLE',     build_platform='darwin', ohnet_plat_dir='Mac'),
        'Mac-x64': dict(endian='LITTLE',     build_platform='darwin', ohnet_plat_dir='Mac'),
        'iOs-ARM': dict(endian='LITTLE',     build_platform='darwin', ohnet_plat_dir='Mac/arm'),
        }

def configure(conf):
    def set_env(varname, value):
        conf.msg(
                'Setting %s to' % varname,
                "True" if value is True else
                "False" if value is False else
                value)
        setattr(conf.env, varname, value)
        return value
    def match_path(paths, message):
        for p in paths:
            try:
                fname = p.format(options=conf.options, debugmode=debugmode, ohnet_plat_dir=ohnet_plat_dir)
                if os.path.exists(fname):
                    return os.path.abspath(fname)
                else:
                    conf.msg("Not found: {0!r}".format(fname))
                    conf.fatal(message)
            except:
                pass
        conf.fatal(message)

    debugmode = conf.options.debugmode
    conf.msg("debugmode:", debugmode)
    dest_platform = conf.options.dest_platform
    if dest_platform is None:
        conf.fatal('Specify --dest-platform')

    platform_info = platforms[dest_platform]
    ohnet_plat_dir = platform_info['ohnet_plat_dir']
    build_platform = platform_info['build_platform']
    endian = platform_info['endian']

    if build_platform != sys.platform:
        conf.fatal('Can only build for {0} on {1}, but currently running on {2}.'.format(dest_platform, build_platform, sys.platform))

    env = conf.env
    append = env.append_value
    env.MSVC_TARGETS = ['x86']
    if dest_platform in ['Windows-x86', 'Windows-x64']:
        conf.load('msvc')
        append('CXXFLAGS',['/MT' if debugmode == 'Release' else '/MTd', '/EHsc'])
        env.LIB_OHNET=['ws2_32', 'iphlpapi', 'dbghelp']
    else:
        conf.load('compiler_cxx')
        append('CXXFLAGS', [
                '-fexceptions', '-Wall', '-pipe',
                '-D_GNU_SOURCE', '-D_REENTRANT', '-DDEFINE_'+endian+'_ENDIAN',
                '-DDEFINE_TRACE', '-fvisibility=hidden', '-Werror'])
        if debugmode == 'Debug':
            append('CXXFLAGS',['-g','-O0'])
        else:
            append('CXXFLAGS',['-O2'])
        append('LINKFLAGS', ['-pthread'])
        if dest_platform in ['Linux-x86', 'Linux-x86', 'Linux-ARM']:
            append('CXXFLAGS',['-Wno-psabi', '-fPIC'])
        elif dest_platform in ['Mac-x86', 'Mac-x64']:
            if dest_platform == 'Mac-x86':
                append('CXXFLAGS', ['-arch', 'i386'])
                append('LINKFLAGS', ['-arch', 'i386'])
            if dest_platform == 'Max-x64':
                append('CXXFLAGS', ['-arch', 'x86_64'])
                append('LINKFLAGS', ['-arch', 'x86_64'])
            append('CXXFLAGS',['-fPIC', '-mmacosx-version-min=10.4', '-DPLATFORM_MACOSX_GNU'])
            append('LINKFLAGS',['-framework', 'CoreFoundation', '-framework', 'SystemConfiguration'])

    set_env('INCLUDES_OHNET', match_path(
        [
            '{options.ohnet_include_dir}',
            '{options.ohnet}/Build/Include',
        ],
        message='Specify --ohnet-include-dir or --ohnet'))
    set_env('STLIBPATH_OHNET', match_path(
        [
            '{options.ohnet_lib_dir}',
            '{options.ohnet}/Build/Obj/{ohnet_plat_dir}/{debugmode}',
        ],
        message='Specify --ohnet-lib-dir or --ohnet'))
    conf.env.STLIB_OHNET=['ohNetProxies', 'TestFramework', 'ohNetCore']

    if conf.options.cross or os.environ.get('CROSS_COMPILE', None):
        cross_compile = conf.options.cross or os.environ['CROSS_COMPILE']
        conf.msg('Cross compiling using compiler prefix:', cross_compile)
        env.CC = cross_compile + 'gcc'
        env.CXX = cross_compile + 'g++'
        env.AR = cross_compile + 'ar'
        env.LINK_CXX = cross_compile + 'g++'
        env.LINK_CC = cross_compile + 'gcc'

def build(bld):
    bld.objects(
            source=[
                'src/CpTopology.cpp',
                'src/CpTopology1.cpp',
                'src/CpTopology2.cpp',
                'src/CpTopology3.cpp',
                'src/CpTopology4.cpp',
            ],
            use=['OHNET'],
            target='objects_topology')
    bld.stlib(
            source=[],
            use=['objects_topology'],
            target='stlib_topology')
    bld.program(source='src/TestTopology1.cpp', use=['OHNET', 'stlib_topology'], target='TestTopology1')
    bld.program(source='src/TestTopology2.cpp', use=['OHNET', 'stlib_topology'], target='TestTopology2')
    bld.program(source='src/TestTopology3.cpp', use=['OHNET', 'stlib_topology'], target='TestTopology3')
    bld.program(source='src/TestTopology4.cpp', use=['OHNET', 'stlib_topology'], target='TestTopology4')
    bld.program(source='src/TestTopology.cpp',  use=['OHNET', 'stlib_topology'], target='TestTopology')
